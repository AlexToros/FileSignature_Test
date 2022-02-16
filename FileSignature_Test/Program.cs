using System;
using System.Collections.Generic;
using System.IO;
using System.Security.Cryptography;

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
			var blockCounter = 0;
			foreach (var block in BlockSequence(stream, blockSize))
			{
				var hash = sha.ComputeHash(block);
				Console.WriteLine($"{blockCounter++:00000}: {Convert.ToBase64String(hash)}");
			}
		}

		static IEnumerable<byte[]> BlockSequence(Stream stream, int blockSize)
		{
			var fullBlocksCount = (int)(stream.Length / blockSize);
			var remind = (int)stream.Length % blockSize;
			using (stream)
			{
				for (int i = 0; i < fullBlocksCount; i++)
				{
					var part = new byte[blockSize];
					stream.Read(part, i * blockSize, blockSize);
					yield return part;
				}

				if (remind > 0)
				{
					var part = new byte[remind];
					stream.Read(part, fullBlocksCount, remind);
					yield return part;
				}
			}
		}
	}
}
