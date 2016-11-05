﻿/* 
MIT License

Copyright (c) 2016 Florian Cäsar, Michael Plainer

For full license see LICENSE in the root directory of this project. 
*/

using log4net;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Sigma.Core.Data.Sources
{
	public class FileSource : IDataSetSource
	{
		private ILog logger = log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

		private bool exists;
		private string fullPath;
		private string localPath;

		private Stream fileStream;

		public FileSource(string path) : this(path, SigmaEnvironment.Globals.Get<string>("workspacePath"))
		{
		}

		public FileSource(string path, string workspace)
		{
			if (path == null)
			{
				throw new ArgumentNullException("Path cannot be null.");
			}

			if (workspace == null)
			{
				throw new ArgumentNullException("Workspace cannot be null (are the SigmaEnironment.Globals missing?)");
			}

			this.localPath = path;
			this.fullPath = workspace + path;

			CheckExists();
		}

		private bool CheckExists()
		{
			return this.exists = File.Exists(fullPath);
		}

		public bool Chunkable
		{
			get { return true; }
		}

		public bool Exists()
		{
			return exists;
		}

		public void Prepare()
		{
			if (!Exists())
			{
				throw new InvalidOperationException($"Cannot prepare file source, underlying file \"{fullPath}\" does not exist.");
			}

			fileStream = new FileStream(fullPath, FileMode.Open);

			logger.Info($"Opened file \"{fullPath}\".");
		}

		public Stream Retrieve()
		{
			if (fileStream == null)
			{
				throw new InvalidOperationException("Cannot retrieve file source, file stream was not established (missing or failed Prepare() call?).");
			}

			return fileStream;
		}
	}
}