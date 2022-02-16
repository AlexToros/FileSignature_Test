using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Threading;

namespace FileSignature_Test
{
	class MyThreadPool<T>
	{
		Action<T> _workTask;
		ConcurrentQueue<T> _contentQueue = new ConcurrentQueue<T>();
		//ConcurrentBag<Thread> _freeThreads = new ConcurrentBag<Thread>();
		//ConcurrentDictionary<int, Thread> _busyThreads = new ConcurrentDictionary<int, Thread>();

		public MyThreadPool(Action<T> workTask)
		{
			_workTask = workTask;
			var mainThread = new Thread(MainLoop);
			mainThread.Start();
		}

		public bool ContentIsOver { get; set; }

		public void AddWorkSource(T source)
		{
			_contentQueue.Enqueue(source);
		}

		private void MainLoop()
		{
			while (_contentQueue.TryDequeue(out T sourceItem) || !ContentIsOver)
			{
				Work(sourceItem);
			}
		}

		private void Work(T sourceItem)
		{
			//if (!_freeThreads.TryTake(out Thread thread))
			var thread = new Thread(BuildThreadStart(sourceItem));
			thread.Start();

			//_busyThreads[thread.ManagedThreadId] = thread;
		}

		private ThreadStart BuildThreadStart(T sourceItem)
		{
			return () =>
			{
				_workTask(sourceItem);
				//var currentThread = Thread.CurrentThread;
				//_busyThreads.TryRemove(currentThread.ManagedThreadId, out var _);
				//_freeThreads.Add(currentThread);
			};
		}
	}

	struct WorkItem
	{
		public WorkItem(int number, byte[] bytes) : this()
		{
			Number = number;
			Bytes = bytes;
		}

		public int Number { get; set; }
		public byte[] Bytes { get; set; }
	}

	class Program
	{
		static void Main(string[] args)
		{
			var filePath = args[0];
			var blockSize = int.Parse(args[1]);
			var stream = File.OpenRead(filePath);

			var workPool = new MyThreadPool<WorkItem>(ComputeAndPrintHash);
			
			foreach (var block in BlockSequence(stream, blockSize))
			{
				workPool.AddWorkSource(block);
			}

			workPool.ContentIsOver = true;
		}

		static void ComputeAndPrintHash(WorkItem item)
		{
			if (item.Bytes == null) return; //I dont know how bytes may be null here. But it is so
			using var sha = SHA256.Create();
			var hash = sha.ComputeHash(item.Bytes);
			Console.WriteLine($"{item.Number:00000}: {Convert.ToBase64String(hash)}");
		}

		static IEnumerable<WorkItem> BlockSequence(Stream stream, int blockSize)
		{
			var fullBlocksCount = (int)(stream.Length / blockSize);
			var remind = (int)stream.Length % blockSize;
			using (stream)
			{
				for (int i = 0; i < fullBlocksCount; i++)
				{
					var part = new byte[blockSize];
					stream.Read(part, 0, blockSize);
					yield return new WorkItem(i + 1, part);
				}

				if (remind > 0)
				{
					var part = new byte[remind];
					stream.Read(part, 0, remind);
					yield return new WorkItem(fullBlocksCount + 1, part);
				}
			}
		}
	}
}
