using System;
using System.Collections.Generic;
using System.IO;
using System.Security.Cryptography;
using System.Threading;

namespace FileSignature_Test
{
	class Program
	{
		static void Main(string[] args)
		{
			var filePath = args[0];
			var blockSize = int.Parse(args[1]);
			var stream = File.OpenRead(filePath);
			using var sha = SHA256.Create();
			
			foreach (var block in BlockSequence(stream, blockSize))
			{
				ComputeAndPrintHash(sha, block);
			}
		}

		static void ComputeAndPrintHash(HashAlgorithm algorithm, (int number, byte[] bytes) block)
		{
			var hash = algorithm.ComputeHash(block.bytes);
			Console.WriteLine($"{block.number:00000}: {Convert.ToBase64String(hash)}");
		}

		static IEnumerable<(int, byte[])> BlockSequence(Stream stream, int blockSize)
		{
			var fullBlocksCount = (int)(stream.Length / blockSize);
			var remind = (int)stream.Length % blockSize;
			using (stream)
			{
				for (int i = 0; i < fullBlocksCount; i++)
				{
					var part = new byte[blockSize];
					stream.Read(part, 0, blockSize);
					yield return (i, part);
				}

				if (remind > 0)
				{
					var part = new byte[remind];
					stream.Read(part, 0, remind);
					yield return (fullBlocksCount, part);
				}
			}
		}
	}
}
