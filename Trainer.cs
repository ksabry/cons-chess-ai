using System;
using System.IO;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using Tensorflow;

namespace Chess
{
	public class Trainer
	{
		private static readonly string versionString = "007";
		private static readonly string saveLocation = "C:\\chess";
		private static readonly int generationSize = 100;
		
		private static readonly (float, float)[][] generationMutations = new (float, float)[][] {
			new (float, float)[] { (0      , 0      ), (0.0001f, 0.0001f), (0.0003f, 0.0003f), (0.0010f, 0.0010f) },
			new (float, float)[] { (0.0001f, 0.0001f), (0.0003f, 0.0003f), (0.0010f, 0.0010f) },
			new (float, float)[] { (0.0003f, 0.0003f), (0.0010f, 0.0010f) },
			new (float, float)[] { (0.0010f, 0.0010f) },
			new (float, float)[] { },
			new (float, float)[] { },
			new (float, float)[] { },
			new (float, float)[] { },
			new (float, float)[] { },
			new (float, float)[] { },
		};

		private readonly Random random;
		private readonly uint randomSeed;

		private NeuralPlayer[] generation;
		private NeuralPlayerSessionManager sessionManager;

		private int generationNumber = 0;

		public Trainer(tensorflow tf, uint seed)
		{
			randomSeed = seed;
			random = new Random((int)seed);
			sessionManager = new NeuralPlayerSessionManager(tf);
		}

		public void CompeteGenerations(int generationNumber0, int generationNumber1)
		{
			var generation0 = LoadGeneration(generationNumber0);
			var generation1 = LoadGeneration(generationNumber1);
			int wins0 = 0;
			int wins1 = 0;

			float points0 = 0;
			float points1 = 0;

			for (int playerIndex0 = 0; playerIndex0 < generation0.Length; playerIndex0++)
			{
				for (int playerIndex1 = 0; playerIndex1 < generation1.Length; playerIndex1++)
				{
					Console.WriteLine($"{(playerIndex0 * generationSize + playerIndex1) * 2} / {generationSize * generationSize * 2} played");
					Console.WriteLine($"Generation {generationNumber0} points: {points0}\nGeneration {generationNumber1} points: {points1}\nGeneration {generationNumber0} wins: {wins0}\nGeneration {generationNumber1} wins: {wins1}\nTies: {2 * generationSize * generationSize - wins0 - wins1}");
					var result = sessionManager.Play(generation0[playerIndex0], generation1[playerIndex1], true);
					
					points0 += result;
					points1 -= result;

					if (result == 1)
					{
						wins0++;
					}
					else if (result == -1)
					{
						wins1++;
					}
					result = sessionManager.Play(generation1[playerIndex1], generation0[playerIndex0], true);

					points1 += result;
					points0 -= result;

					if (result == 1)
					{
						wins1++;
					}
					else if (result == -1)
					{
						wins0++;
					}
				}
			}
			Console.WriteLine($"Generation {generationNumber0} points: {points0}\nGeneration {generationNumber1} points: {points1}\nGeneration {generationNumber0} wins: {wins0}\nGeneration {generationNumber1} wins: {wins1}\nTies: {2 * generationSize * generationSize - wins0 - wins1}");
		}

		public void Train()
		{
			if (!RestoreLastGeneration())
			{
				Console.WriteLine("No previous generations found; starting at generation 1");
				generation = new NeuralPlayer[generationSize];
				for (int playerIndex = 0; playerIndex < generationSize; playerIndex++)
				{
					generation[playerIndex] = new NeuralPlayer(random.Next());
				}
			}
			else
			{
				Console.WriteLine($"Starting from saved generation {generationNumber}");
			}

			while (generationNumber < 1000)
			{
				generationNumber++;
				generation = IterateGeneration();
				SaveCurrentGeneration();
			}
		}

		private NeuralPlayer[] IterateGeneration()
		{
			int gamesPlayed = 0;
			var scores = new float[generationSize];
			for (int whitePlayerIndex = 0; whitePlayerIndex < generationSize; whitePlayerIndex++)
			{
				for (int blackPlayerIndex = 0; blackPlayerIndex < generationSize; blackPlayerIndex++)
				{
					Console.Write($"Generation {generationNumber} scoring: {gamesPlayed} / {generationSize * generationSize - generationSize}\r");

					if (whitePlayerIndex == blackPlayerIndex)
					{
						continue;
					}

					// TODO: we could randomly make the first several moves to add some variance in the runs (obviously we would symmetrically play each match on both sides)
					var result = sessionManager.Play(generation[whitePlayerIndex], generation[blackPlayerIndex], true);
					scores[whitePlayerIndex] += result;
					scores[blackPlayerIndex] -= result;
					gamesPlayed++;
				}
			}
			
			Console.Write($"Generation {generationNumber} scoring complete              \n");
			string scoresString = "";
			for (int i = 0; i < scores.Length; i++)
			{
				if (i != 0)
				{
					scoresString += ", ";
				}
				scoresString += scores[i].ToString();
			}
			Console.WriteLine($"Scores: {scoresString}");

			// Mutate generation

			var sortedPlayers = new List<(float, int)>();
			for (int playerIndex = 0; playerIndex < generationSize; playerIndex++)
			{
				// -score for descending order
				sortedPlayers.Add((-scores[playerIndex], playerIndex));
			}
			sortedPlayers.Sort();

			var newGeneration = new List<NeuralPlayer>();
			for (int sortedIndex = 0; sortedIndex < generationSize; sortedIndex++)
			{
				(var _, var playerIndex) = sortedPlayers[sortedIndex];
				foreach ((var weightMutationAmount, var biasMutationAmount) in generationMutations[sortedIndex])
				{
					newGeneration.Add(Mutate(generation[playerIndex], weightMutationAmount, biasMutationAmount));
				}
			}

			// We shuffle the new generation since we don't want an advantage based upon order of the players in generation which would be the case due to how the score sorting works
			Shuffle(newGeneration);
			return newGeneration.ToArray();
		}

		private void Shuffle<T>(IList<T> list)
		{
			int n = list.Count;
			while (n > 1)
			{
				n--;
				int k = random.Next(n + 1);
				T value = list[k];
				list[k] = list[n];
				list[n] = value;
			}
		}

		private NeuralPlayer Mutate(NeuralPlayer player, float weightMutationAmount, float biasMutationAmount)
		{
			var newWeights = new float[player.weights.Length];
			var newBiases = new float[player.biases.Length];
			Array.Copy(player.weights, newWeights, player.weights.Length);
			Array.Copy(player.biases, newBiases, player.biases.Length);

			for (int weightIndex = 0; weightIndex < newWeights.Length; weightIndex++)
			{
				newWeights[weightIndex] += weightMutationAmount * ((float)random.NextDouble() * 2 - 1);
			}
			for (int biasIndex = 0; biasIndex < newBiases.Length; biasIndex++)
			{
				newBiases[biasIndex] += biasMutationAmount * ((float)random.NextDouble() * 2 - 1);
			}

			return new NeuralPlayer(newWeights, newBiases);
		}

		public void TrainPairwise()
		{
			if (!RestoreLastGeneration())
			{
				Console.WriteLine("No previous generations found; starting at generation 1");
				generation = new NeuralPlayer[generationSize];
				for (int playerIndex = 0; playerIndex < generationSize; playerIndex++)
				{
					generation[playerIndex] = new NeuralPlayer(random.Next());
				}
			}
			else
			{
				Console.WriteLine($"Starting from saved generation {generationNumber}");
			}

			while (generationNumber < 1000)
			{
				generationNumber++;
				generation = IterateGenerationPairwise();
				SaveCurrentGeneration();
			}
		}

		private NeuralPlayer[] IterateGenerationPairwise()
		{
			int gamesPlayed = 0;
			
			var indexPairs = new List<int>();
			for (int playerIndex = 0; playerIndex < generationSize; playerIndex++)
			{
				indexPairs.Add(playerIndex);
			}
			Shuffle(indexPairs);

			var nextGeneration = new List<NeuralPlayer>();

			for (int pairIndex = 0; pairIndex < generationSize; pairIndex += 2)
			{
				var whitePlayerIndex = indexPairs[pairIndex];
				var blackPlayerIndex = indexPairs[pairIndex + 1];
				var whitePlayer = generation[whitePlayerIndex];
				var blackPlayer = generation[blackPlayerIndex];

				Console.Write($"Generation {generationNumber} scoring: {gamesPlayed} / {generationSize / 2}\r");

				// We could play both sides possibly
				var result = sessionManager.Play(whitePlayer, blackPlayer, true);

				if (result > 0)
				{
					// White performed better
					var newPlayer = MutatePairLoser(whitePlayer, blackPlayer, result * 0.5f, 0.01f, 0.1f);
					nextGeneration.Add(whitePlayer);
					nextGeneration.Add(newPlayer);
				}
				else if (result < 0)
				{
					// Black performed better
					var newPlayer = MutatePairLoser(blackPlayer, whitePlayer, -result * 0.5f, 0.01f, 0.1f);
					nextGeneration.Add(blackPlayer);
					nextGeneration.Add(newPlayer);
				}
				else
				{
					// TODO: there's an argument to made for mutating in this case
					nextGeneration.Add(whitePlayer);
					nextGeneration.Add(blackPlayer);
				}

				gamesPlayed++;
			}

			return nextGeneration.ToArray();
		}

		private NeuralPlayer MutatePairLoser(NeuralPlayer winner, NeuralPlayer loser, float winnerShareRate, float mutationRate, float mutationAmount)
		{
			var newWeights = new float[loser.weights.Length];
			var newBiases = new float[loser.biases.Length];
			Array.Copy(loser.weights, newWeights, loser.weights.Length);
			Array.Copy(loser.biases, newBiases, loser.biases.Length);

			for (int weightIndex = 0; weightIndex < newWeights.Length; weightIndex++)
			{
				if (random.NextDouble() < winnerShareRate)
				{
					newWeights[weightIndex] = winner.weights[weightIndex];
				}
				if (random.NextDouble() < mutationRate)
				{
					newWeights[weightIndex] += mutationAmount * ((float)random.NextDouble() * 2 - 1);
				}
			}
			for (int biasIndex = 0; biasIndex < newBiases.Length; biasIndex++)
			{
				if (random.NextDouble() < winnerShareRate)
				{
					newBiases[biasIndex] = winner.biases[biasIndex];
				}
				if (random.NextDouble() < mutationRate)
				{
					newBiases[biasIndex] += mutationAmount * ((float)random.NextDouble() * 2 - 1);
				}
			}
			return new NeuralPlayer(newWeights, newBiases);
		}

		private void SaveCurrentGeneration()
		{
			for (int playerIndex = 0; playerIndex < generationSize; playerIndex++)
			{
				generation[playerIndex].Save(PlayerFilename(generationNumber, playerIndex));
			}
		}

		private bool RestoreLastGeneration()
		{
			var savedGenerations = new SortedDictionary<int, bool[]>();
			foreach (var playerFilename in Directory.GetFiles(saveLocation))
			{
				FileInfo fileInfo = new FileInfo(playerFilename);
				var matchResult = Regex.Match(fileInfo.Name, @"^player_(\d+)_(\d+)_(\d+)_(\d+)$");
				if (!matchResult.Success)
				{
					Console.WriteLine($"Warning: invalid filename encountered in saved players directory: {playerFilename}");
					continue;
				}

				string matchVersion = matchResult.Groups[1].Value;
				uint matchSeed = uint.Parse(matchResult.Groups[2].Value);
				int matchGenerationNumber = int.Parse(matchResult.Groups[3].Value);
				int matchPlayerIndex = int.Parse(matchResult.Groups[4].Value);
				
				if (matchVersion != versionString || matchSeed != randomSeed)
				{
					continue;
				}

				// Negative generation number for descending order
				if (!savedGenerations.ContainsKey(-matchGenerationNumber))
				{
					savedGenerations.Add(-matchGenerationNumber, new bool[generationSize]);
				}
				if (matchPlayerIndex >= generationSize)
				{
					Console.WriteLine($"Warning: ignoring saved player with higher index than generation count: {playerFilename}");
				}
				savedGenerations.GetValueOrDefault(-matchGenerationNumber)[matchPlayerIndex] = true;
			}
			foreach (int key in savedGenerations.Keys)
			{
				bool[] playersFound = savedGenerations.GetValueOrDefault(key);
				if (Array.TrueForAll(playersFound, p => p))
				{
					generation = LoadGeneration(-key);
					return true;
				}
				else
				{
					Console.WriteLine($"Warning: found some but not all of generation {-key}; attempting latest previous generation");
				}
			}
			return false;
		}

		private NeuralPlayer[] LoadGeneration(int generationNumber)
		{
			var loadedGeneration = new NeuralPlayer[generationSize];
			for (int playerIndex = 0; playerIndex < generationSize; playerIndex++)
			{
				loadedGeneration[playerIndex] = NeuralPlayer.Load(PlayerFilename(generationNumber, playerIndex));
			}
			this.generationNumber = generationNumber;
			return loadedGeneration;
		}

		private string PlayerFilename(int generationNumber, int playerIndex)
		{
			return $"{saveLocation}\\player_{versionString}_{randomSeed.ToString().PadLeft(10, '0')}_{generationNumber.ToString().PadLeft(3, '0')}_{playerIndex.ToString().PadLeft(3, '0')}";
		}
	}
}
