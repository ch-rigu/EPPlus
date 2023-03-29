﻿/*************************************************************************************************
  Required Notice: Copyright (C) EPPlus Software AB. 
  This software is licensed under PolyForm Noncommercial License 1.0.0 
  and may only be used for noncommercial purposes 
  https://polyformproject.org/licenses/noncommercial/1.0.0/

  A commercial license to use this software can be purchased at https://epplussoftware.com
 *************************************************************************************************
  Date               Author                       Change
 *************************************************************************************************
  22/3/2023         EPPlus Software AB           EPPlus v7
 *************************************************************************************************/
using OfficeOpenXml.FormulaParsing.Excel.Functions.Metadata;
using OfficeOpenXml.FormulaParsing.Excel.Functions.RefAndLookup.LookupUtils;
using OfficeOpenXml.FormulaParsing.FormulaExpressions;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;

namespace OfficeOpenXml.FormulaParsing.Excel.Functions.RefAndLookup
{
    [FunctionMetadata(
        Category = ExcelFunctionCategory.LookupAndReference,
        EPPlusVersion = "7",
        Description = "Looks up a Searches for a specified item in an array or range of cells, and returns the item's relative position.")]
    internal class XMatch : ExcelFunction
    {
        private Stopwatch _stopwatch = null;

        private int GetMatchIndex(object lookupValue, IRangeInfo lookupRange, bool asc, LookupMatchMode matchMode)
        {
            var comparer = new LookupComparer(matchMode);
            var ix = LookupBinarySearch.BinarySearch(lookupValue, lookupRange, asc, comparer);
            if (ix == 0)
            {
                return ix;
            }
            var result = ix < 0 ? ~ix : ix;
            if (matchMode == LookupMatchMode.ExactMatchReturnNextSmaller)
            {
                return result - 1;
            }
            if (matchMode == LookupMatchMode.ExactMatchReturnNextLarger)
            {
                return result;
            }
            return -1;
        }

        public override CompileResult Execute(IEnumerable<FunctionArgument> arguments, ParsingContext context)
        {
            if (context.Debug)
            {
                _stopwatch = new Stopwatch();
                _stopwatch.Start();
            }
            ValidateArguments(arguments, 2);
            var lookupValue = arguments.ElementAt(0).Value;

            // lookup range
            if (!arguments.ElementAt(1).IsExcelRange) return CreateResult(eErrorType.Value);
            var lookupRange = arguments.ElementAt(1).ValueAsRangeInfo;
            var lookupDirection = XlookupUtil.GetLookupDirection(lookupRange);
            if (lookupRange.Size.NumberOfRows > 1 && lookupRange.Size.NumberOfCols > 1) return CreateResult(eErrorType.Value);

            // match mode
            var matchMode = LookupMatchMode.ExactMatch;
            if (arguments.Count() > 2 && arguments.ElementAt(2) != null)
            {
                var mm = ArgToInt(arguments, 2);
                matchMode = XlookupUtil.GetMatchMode(mm);
            }

            // search mode
            var searchMode = LookupSearchMode.StartingAtFirst;
            if (arguments.Count() > 3)
            {
                var sm = ArgToInt(arguments, 3);
                searchMode = XlookupUtil.GetSearchMode(sm);
            }
            int ix;
            if (searchMode == LookupSearchMode.BinarySearchAscending || searchMode == LookupSearchMode.BinarySearchDescending)
            {
                var asc = searchMode == LookupSearchMode.BinarySearchAscending;
                ix = GetMatchIndex(lookupValue, lookupRange, asc, matchMode);
            }
            else
            {
                var scanner = new XlookupScanner(lookupValue, lookupRange, searchMode, matchMode);
                ix = scanner.FindIndex();
            }
            if (context.Debug)
            {
                _stopwatch.Stop();
                context.Configuration.Logger.LogFunction("XMATCH", _stopwatch.ElapsedMilliseconds);
            }
            if (ix < 0)
            {
                return CreateResult(eErrorType.NA);
            }
            return CreateResult(ix + 1, DataType.Integer);
        }
    }
}
