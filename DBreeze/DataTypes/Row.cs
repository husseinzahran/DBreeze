﻿/* 
  Copyright (C) 2012 dbreeze.tiesky.com / Alex Solovyov / Ivars Sudmalis.
  It's a free software for those, who thinks that it should be free.
*/

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using DBreeze.LianaTrie;
using DBreeze.DataTypes;
using DBreeze.Utils;

namespace DBreeze.DataTypes
{
    public class Row<TKey,TValue>
    {
        //LTrieRootNode _root = null;
        //byte[] _ptrToValue = null;
        bool _exists = false;
        bool _useCache = false;

        internal NestedTable nestedTable = null;

        TKey _key;
        LTrie _masterTrie = null;
        LTrieRow _row = null;

        //byte[] btKey = null; 

        public Row(LTrieRow row, LTrie masterTrie, bool useCache)
        {
            if (row == null)
                _exists = false;
            else
            {
                _row = row;
                //_root = row._root;
                //_ptrToValue = row.LinkToValue;
                _exists = row.Exists;
            }
            _useCache = useCache;
            _masterTrie = masterTrie;

            if (_exists)
                _key = DataTypesConvertor.ConvertBack<TKey>(row.Key);
        }

        ///// <summary>
        ///// 
        ///// </summary>
        ///// <param name="root"></param>
        ///// <param name="ptrToValue"></param>
        ///// <param name="exists"></param>
        ///// <param name="key"></param>
        ///// <param name="useCache">(useCache == true) ? READ : READ_SYNCHRO   models</param>
        //public Row(LTrieRootNode root, byte[] ptrToValue, bool exists,byte[] key,bool useCache)
        //{
        //    _root = root;
        //    _ptrToValue = ptrToValue;
        //    _exists = exists;
        //    _useCache = useCache;
        //    //btKey = key;

        //    if (_exists)
        //        _key = DataTypesConvertor.ConvertBack<TKey>(key);
        //}


        public bool Exists
        {
            get { return _exists; }
        }

        public TKey Key
        {
            get
            {
                return _key;
            }
        }

        /// <summary>
        /// We are inside of the row.
        /// <para>This Method will give you ability to the nested tables which can be stored inside of table by tableIndex</para>
        /// </summary>
        /// <returns></returns>
        public NestedTable GetTable(uint tableIndex)
        {
            if (!_exists)
                return new NestedTable(null,false,false);

            ///////////  FOR NOW allow insert from master is always false, later we have to change Transaction.Insert, and insertPart to return also a row???

            ///FOR NOW NOW DIFFERENCE
            if(nestedTable == null)
            {
                //Master row select
                return _row.Root.Tree.GetTable(_row, ref _row.Key, tableIndex, _masterTrie, false, this._useCache);
            }
            else
            {
                
                //Nested table
                return _row.Root.Tree.GetTable(_row, ref _row.Key, tableIndex, _masterTrie, nestedTable._insertAllowed, this._useCache);
            }

            
            //Check if table exists
            //must be supplied horizontal index

            //2 cases one is master table, second is nested table

            //Nested Table we can get in master via SelectTable


            //if (nestedTable == null)
            //    return new NestedTable(null, false, false);

            //return this.nestedTable;//.GetTable<TNestedKey>(TNestedKey
        }
        

        /// <summary>
        /// Returns partial value representation starting from specif index and specified length.
        /// <para>To get full value as byte[] use GetValuePart(0)</para>
        /// </summary>
        /// <param name="startIndex"></param>
        /// <param name="length"></param>
        /// <returns></returns>
        public byte[] GetValuePart(uint startIndex, uint length)
        {
            if (_exists)
            {
                //Bringing arguments to int scope
                if ((startIndex + length) > Int32.MaxValue)
                {
                    length = Int32.MaxValue - startIndex;
                }

                long valueStartPointer = 0;
                uint valueFullLength = 0;
                //return this._root.Tree.Cache.ReadValuePartially(this._ptrToValue, startIndex, length, this._useCache, out valueStartPointer, out valueFullLength);
                return this._row.Root.Tree.Cache.ReadValuePartially(this._row.LinkToValue, startIndex, length, this._useCache, out valueStartPointer, out valueFullLength);
            }

            return null;
        }

        /// <summary>
        /// Returns partial value representation starting from specif index till and till the end of value.
        /// </summary>
        /// <param name="startIndex"></param>
        /// <returns></returns>
        public byte[] GetValuePart(uint startIndex)
        {
            uint length = Int32.MaxValue;

            if (_exists)
            {
                //Bringing arguments to int scope
                if ((startIndex + length) > Int32.MaxValue)
                {
                    length = Int32.MaxValue - startIndex;
                }


                long valueStartPointer = 0;
                uint valueFullLength = 0;
                return this._row.Root.Tree.Cache.ReadValuePartially(this._row.LinkToValue, startIndex, length, this._useCache, out valueStartPointer, out valueFullLength);
            }

            return null;
        }

        /// <summary>
        /// Returns physical link to key/value if it exists, otherwise null,
        /// this link can be used by SelectDirect (always returns 8 bytes)
        /// </summary>
        public byte[] LinkToValue
        {
            get
            {
                if (_exists)
                {
                    return this._row.LinkToValue.EnlargeByteArray_BigEndian(8);
                }

                return null;
            }
        }

        /// <summary>
        /// Returns datablock which identifier is stored in this row from specified index.
        /// <para></para>
        /// Insert dynamic lenght datablock is possible via tran.InsertDataBlock or NestedTable.InsertDataBlock.
        /// <para></para>
        /// can return null.
        /// </summary>
        /// <param name="dataBlockId"></param>
        /// <returns></returns>
        public byte[] GetDataBlock(uint startIndex)
        {
            if (_exists)
            {
                long valueStartPointer = 0;
                uint valueFullLength = 0;
                byte[] dataBlockId = this._row.Root.Tree.Cache.ReadValuePartially(this._row.LinkToValue, startIndex, 16, this._useCache, out valueStartPointer, out valueFullLength);
                return this._row.Root.Tree.Cache.ReadDynamicDataBlock(ref dataBlockId, this._useCache);
            }

            return null;
        }

       
        /// <summary>
        /// Returns full value and converts it to the value data type.
        /// <para>To take full value or part of the value as byte[] use GetValuePart or GetBytes (for string types like DbAscii etc.)</para>
        /// <para>If your value contains serialized object inside or it's a string type (like DbAscii etc.), use Value.Get property.</para>
        /// </summary>
        /// <returns></returns>
        public TValue Value
        {
            get
            {
                if (_exists)
                {
                    long valueStartPointer = 0;
                    uint valueFullLength = 0;
                    //Console.WriteLine("UseCache " + this._useCache);
                    byte[] res = this._row.Root.Tree.Cache.ReadValue(this._row.LinkToValue, this._useCache, out valueStartPointer, out valueFullLength);
                    //Console.WriteLine("Res " + res.ToBytesString(""));
                    return DataTypesConvertor.ConvertBack<TValue>(res);
                }

                return default(TValue);
            }
        }






        public void PrintOut()
        {
            PrintOut(String.Empty);
        }
        /// <summary>
        /// Experimantal Console PrintOut
        /// </summary>
        public void PrintOut(string leadingText)
        {
            if (!_exists)
                Console.WriteLine("Key doesn't exist");
            else
            {
                

                if (typeof(TKey) == DBreeze.DataTypes.DataTypesConvertor.TYPE_BYTE_ARRAY)
                {
                    Console.WriteLine("{2}; K: {0}; V: {1}", this._key, Value,leadingText);
                }
                else
                {
                    Console.WriteLine("{2}; K: {0}; V: {1}", this._key, Value, leadingText);
                }
            }
        }

    }
}
