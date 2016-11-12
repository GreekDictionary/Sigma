﻿/* 
MIT License

Copyright (c) 2016 Florian Cäsar, Michael Plainer

For full license see LICENSE in the root directory of this project. 
*/

using log4net;
using Sigma.Core.Handlers;
using Sigma.Core.Math;
using Sigma.Core.Utils;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;

namespace Sigma.Core.Data.Extractors
{
	/// <summary>
	/// A CSV record extractor, which extracts string based records as columns.
	/// </summary>
	public class CSVRecordExtractor : BaseExtractor
	{
		private ILog logger = log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

		private Dictionary<string, IList<int>> namedColumnIndexMappings;
		private Dictionary<int, Dictionary<object, object>> columnValueMappings;

		public Dictionary<string, IList<int>> NamedColumnIndexMapping
		{
			get { return namedColumnIndexMappings; }
		}

		public override string[] SectionNames { get; protected set; }

		/// <summary>
		/// Create a CSV record extractor with a certain named column index mapping and an optional value mapping.
		/// </summary>
		/// <param name="namedColumnIndexMappings">The named column index mapping (not flattened).</param>
		/// <param name="columnValueMappings">The optional value mapping.</param>
		public CSVRecordExtractor(Dictionary<string, int[][]> namedColumnIndexMappings, Dictionary<int, Dictionary<object, object>> columnValueMappings = null) : this(ArrayUtils.GetFlatColumnMappings(namedColumnIndexMappings))
		{
		}

		/// <summary>
		/// Create a CSV record extractor with a certain named column index mapping and an optional value mapping.
		/// </summary>
		/// <param name="namedColumnIndexMappings">The named column index mapping (flattened).</param>
		/// <param name="columnValueMappings">The optional value mapping.</param>
		public CSVRecordExtractor(Dictionary<string, IList<int>> namedColumnIndexMappings, Dictionary<int, Dictionary<object, object>> columnValueMappings = null)
		{
			this.namedColumnIndexMappings = namedColumnIndexMappings;

			if (columnValueMappings == null)
			{
				columnValueMappings = new Dictionary<int, Dictionary<object, object>>();
			}

			if (namedColumnIndexMappings.Count == 0)
			{
				throw new ArgumentException("There must be at least one named column index mapping, but the given dictionary was empty.");
			}

			this.columnValueMappings = columnValueMappings;
			this.SectionNames = namedColumnIndexMappings.Keys.ToArray();
		}

		/// <summary>
		/// Add a value mapping for a certain column and certain values, which will automatically be assigned to numbers (respective to their order in the list). 
		/// </summary>
		/// <param name="column">The column to add the value mapping to.</param>
		/// <param name="objects">The values to map.</param>
		/// <returns>This record extractor (for convenience).</returns>
		public CSVRecordExtractor AddValueMapping(int column, params object[] objects)
		{
			return AddValueMapping(column, mapping: ArrayUtils.MapToOrder(objects));
		}

		/// <summary>
		/// Add a value mapping for a certain column and certain key value pairs. Each key will be replaced with its value during extraction. 
		/// </summary>
		/// <param name="column">The column to add the value mapping to.</param>
		/// <param name="objects">The values to map.</param>
		/// <returns>This record extractor (for convenience).</returns>
		public CSVRecordExtractor AddValueMapping(int column, Dictionary<object, object> mapping)
		{
			this.columnValueMappings.Add(column, mapping);

			return this;
		}

		public override Dictionary<string, INDArray> ExtractDirect(int numberOfRecords, IComputationHandler handler)
		{
			if (Reader == null)
			{
				throw new InvalidOperationException("Cannot extract from record extractor before attaching a reader (reader was null).");
			}

			if (handler == null)
			{
				throw new ArgumentNullException("Computation handler cannot be null.");
			}

			if (numberOfRecords <= 0)
			{
				throw new ArgumentException($"Number of records to read must be > 0 but was {numberOfRecords}.");
			}

			return ExtractFrom(Reader.Read(numberOfRecords), numberOfRecords, handler);
		}

		public override Dictionary<string, INDArray> ExtractFrom(object readData, int numberOfRecords, IComputationHandler handler)
		{
			//read data being null indicates that nothing could be read so we can't extract anything either
			if (readData == null)
			{
				return null;
			}

			string[][] lineParts = (string[][]) readData;

			int readNumberOfRecords = lineParts.Length;

			logger.Info($"Extracting {readNumberOfRecords} records from reader {Reader} (requested: {numberOfRecords})...");

			Dictionary<string, INDArray> namedArrays = new Dictionary<string, INDArray>();

			foreach (string name in namedColumnIndexMappings.Keys)
			{
				IList<int> mappings = namedColumnIndexMappings[name];
				INDArray array = handler.Create(readNumberOfRecords, 1, mappings.Count);
				TypeConverter converter = TypeDescriptor.GetConverter(typeof(double));

				for (int i = 0; i < readNumberOfRecords; i++)
				{
					for (int y = 0; y < mappings.Count; y++)
					{
						int column = mappings[y];
						string value = lineParts[i][column];

						try
						{
							if (columnValueMappings.ContainsKey(column) && columnValueMappings[column].ContainsKey(value))
							{
								array.SetValue(columnValueMappings[column][value], i, 0, y);
							}
							else
							{
								array.SetValue(converter.ConvertFromString(value), i, 0, y);
							}
						}
						catch (NotSupportedException)
						{
							throw new FormatException($"Cannot convert value \"{value}\" of type {value.GetType()} to double for further processing (are you missing a column value mapping?).");
						}
					}
				}

				namedArrays.Add(name, array);
			}

			logger.Info($"Done extracting {readNumberOfRecords} records from reader {Reader} (requested: {numberOfRecords}).");

			return namedArrays;
		}

		public override void Dispose()
		{
		}

		public static Dictionary<string, IList<int>> ParseExtractorParameters(object[] parameters)
		{
			if (parameters.Length == 0)
			{
				throw new ArgumentException("Extractor parameters cannot be empty.");
			}

			Dictionary<string, IList<int>> columnMappings = new Dictionary<string, IList<int>>();

			string currentNamedSection = null;
			IList<int> currentColumnMapping = null;
			int paramIndex = 0;

			foreach (object param in parameters)
			{
				if (param is string)
				{
					currentNamedSection = (string) param;

					if (columnMappings.ContainsKey(currentNamedSection))
					{
						throw new ArgumentException($"Named sections can only be used once, but section {currentNamedSection} (argument {paramIndex}) was already used.");
					}

					currentColumnMapping = new List<int>();
					columnMappings.Add(currentNamedSection, currentColumnMapping);
				}
				else if (param is int || param is int[])
				{
					if (currentNamedSection == null)
					{
						throw new ArgumentException("Cannot assign parameters without naming a section.");
					}

					if (param is int)
					{
						currentColumnMapping.Add((int) param);
					}
					else
					{
						int[] range = (int[]) param;

						if (range.Length != 2)
						{
							throw new ArgumentException($"Column ranges can only be pairs (size 2), but array length of argument {paramIndex} was {range.Length}.");
						}

						int[] flatRange = ArrayUtils.Range(range[0], range[1]);

						for (int i = 0; i < flatRange.Length; i++)
						{
							currentColumnMapping.Add(flatRange[i]);
						}
					}
				}
				else
				{
					throw new ArgumentException("All parameters must be either of type string, int or int[]");
				}

				paramIndex++;
			}

			return columnMappings;
		}
	}
}
