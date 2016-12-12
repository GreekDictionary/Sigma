﻿/* 
MIT License

Copyright (c) 2016 Florian Cäsar, Michael Plainer

For full license see LICENSE in the root directory of this project. 
*/

using NUnit.Framework;
using Sigma.Core;
using Sigma.Core.Data.Datasets;
using Sigma.Core.Data.Extractors;
using Sigma.Core.Data.Iterators;
using Sigma.Core.Data.Readers;
using Sigma.Core.Data.Sources;
using Sigma.Core.Handlers;
using Sigma.Core.Handlers.Backends;
using Sigma.Core.Utils;
using System;
using System.Collections.Generic;
using System.IO;
using Sigma.Core.Handlers.Backends.DiffSharp.NativeCpu;

namespace Sigma.Tests.Data.Iterators
{
	public class TestUnifiedIterator
	{
		private static void CreateCsvTempFile(string name)
		{
			File.Create(Path.GetTempPath() + name).Dispose();
			File.WriteAllLines(Path.GetTempPath() + name, new[] { "5.1,3.5,1.4,0.2,Iris-setosa", "4.9,3.0,1.4,0.2,Iris-setosa", "4.7,3.2,1.3,0.2,Iris-setosa" });
		}

		private static void DeleteTempFile(string name)
		{
			File.Delete(Path.GetTempPath() + name);
		}

		[TestCase]
		public void TestUnifiedIteratorCreate()
		{
			Assert.Throws<ArgumentNullException>(() => new UnifiedIterator(null));
		}

		[TestCase]
		public void TestUnifiedIteratorYield()
		{
			string filename = ".unittestfile" + nameof(TestUnifiedIteratorYield);

			CreateCsvTempFile(filename);

			SigmaEnvironment.Clear();

			FileSource source = new FileSource(filename, Path.GetTempPath());
			CsvRecordExtractor extractor = (CsvRecordExtractor) new CsvRecordReader(source).Extractor(new CsvRecordExtractor(new Dictionary<string, int[][]> { ["inputs"] = new[] { new[] { 0 } } }));
			Dataset dataset = new Dataset("test", 2, new DiskCacheProvider(Path.GetTempPath() + "/" + nameof(TestUnifiedIteratorYield)), true, extractor);
			UnifiedIterator iterator = new UnifiedIterator(dataset);
			SigmaEnvironment sigma = SigmaEnvironment.Create("test");
			IComputationHandler handler = new CpuFloat32Handler();

			foreach (var block in iterator.Yield(handler, sigma))
			{
				Assert.AreEqual(new float[] {5.1f, 4.9f, 4.7f}, block["inputs"].GetDataAs<float>().GetValuesArrayAs<float>(0, 3));
			}

			dataset.Dispose();

			DeleteTempFile(filename);
		}
	}
}