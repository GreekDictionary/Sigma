﻿/* 
MIT License

Copyright (c) 2016-2017 Florian Cäsar, Michael Plainer

For full license see LICENSE in the root directory of this project. 
*/

using Sigma.Core.Data.Extractors;
using Sigma.Core.Handlers;
using Sigma.Core.MathAbstract;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Sigma.Core.Data.Preprocessors
{
	/// <summary>
	/// The base class for all preprocessors. Takes care of selective per section processing and simplifies implementation of new preprocessors. 
	/// </summary>
	[Serializable]
	public abstract class BasePreprocessor : BaseExtractor, IRecordPreprocessor
	{
		/// <summary>
		/// Similar to <see cref="IRecordExtractor.SectionNames"/>, this specifies the specific sections processed in this extractor or null if all given sections are processed.
		/// </summary>
		public IReadOnlyCollection<string> ProcessedSectionNames { get; protected set; }

	    /// <inheritdoc />
	    public abstract bool AffectsDataShape { get; }

		/// <summary>
		/// Create a base processor with an optional array of sections to process.
		/// If an array of section names is specified, only the sections with those names are processed. 
		/// If no such array is specified (null or empty), all sections are processed.
		/// </summary>
		/// <param name="processedSectionNames">The section names to process in this preprocessor (all if null or empty).</param>
		protected BasePreprocessor(string[] processedSectionNames = null)
		{
			if (processedSectionNames != null && processedSectionNames.Length == 0)
			{
				processedSectionNames = null;
			}

			ProcessedSectionNames = processedSectionNames;
		}

	    /// <inheritdoc />
	    public override Dictionary<string, INDArray> ExtractDirectFrom(object readData, int numberOfRecords, IComputationHandler handler)
		{
			Dictionary<string, INDArray> unprocessedNamedArrays = (Dictionary<string, INDArray>) readData;
			Dictionary<string, INDArray> processedNamedArrays = new Dictionary<string, INDArray>();

			foreach (string sectionName in unprocessedNamedArrays.Keys)
			{
				INDArray processedArray = unprocessedNamedArrays[sectionName];

				if (processedArray.Shape[0] != numberOfRecords)
				{
					long[] beginIndices = (long[]) processedArray.Shape.Clone();
					long[] endIndices = (long[]) processedArray.Shape.Clone();

					beginIndices = NDArrayUtils.GetSliceIndicesAlongDimension(0, 0, beginIndices, copyResultShape: false, sliceEndIndex: false);
					endIndices = NDArrayUtils.GetSliceIndicesAlongDimension(0, Math.Min(numberOfRecords, processedArray.Shape[0]), endIndices, copyResultShape: false, sliceEndIndex: true);

					processedArray = processedArray.Slice(beginIndices, endIndices);
				}

				if (ProcessedSectionNames == null || ProcessedSectionNames.Contains(sectionName))
				{
					processedArray = ProcessDirect(processedArray, handler);
				}

				processedNamedArrays.Add(sectionName, processedArray);
			}

			return processedNamedArrays;
		}

		/// <summary>
		/// Process a certain ndarray with a certain computation handler.
		/// </summary>
		/// <param name="array">The ndarray to process.</param>
		/// <param name="handler">The computation handler to do the processing with.</param>
		/// <returns>An ndarray with the processed contents of the given array (can be the same or a new one).</returns>
		internal abstract INDArray ProcessDirect(INDArray array, IComputationHandler handler);

	    /// <inheritdoc />
	    public override void Dispose()
		{
			// there shouldn't be anything to dispose in a preprocessor
		}
	}
}
