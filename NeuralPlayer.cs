using System;
using System.IO;
using System.Collections.Generic;
using NumSharp;
using Tensorflow;

namespace Chess
{
	// Ideas/Questions:
	//		we should add a network input for the depth the search is currently at, or maybe disincentivize directly
	//		can we batch the board evaluations?

	public class NeuralPlayerSessionManager
	{
		private static readonly int maxGameMoveCount = 100;
		private static readonly int maxMoveEvaluationsCount = 500;

		private static readonly int inputLayerSize = 2 * 64 * 13 + 1;
		private static readonly int[] sharedLayerSizes = new int[] { 1024, 512, 256, 256 };
		private static readonly int[] terminalValueLayerSizes = new int[] { 256 };
		private static readonly int[] whiteMoveValueLayerSizes = new int[] { 256 };
		private static readonly int[] blackMoveValueLayerSizes = new int[] { 256 };
		private static readonly int outputTerminalValueLayerSize = 1;
		private static readonly int outputWhiteMoveValueLayerSize = 1;
		private static readonly int outputBlackMoveValueLayerSize = 1;

		private Tensor input;
		private Tensor whiteOutput;
		private Tensor blackOutput;

		private readonly ResourceVariable whiteWeight;
		private readonly ResourceVariable whiteBias;
		private readonly ResourceVariable blackWeight;
		private readonly ResourceVariable blackBias;

		private readonly Tensor whiteWeightPlaceholder;
		private readonly Tensor whiteBiasPlaceholder;
		private readonly Tensor blackWeightPlaceholder;
		private readonly Tensor blackBiasPlaceholder;

		private readonly Tensor whiteWeightAssignment;
		private readonly Tensor whiteBiasAssignment;
		private readonly Tensor blackWeightAssignment;
		private readonly Tensor blackBiasAssignment;

		private readonly Session session;

		public NeuralPlayerSessionManager(tensorflow tf)
		{
			session = tf.Session();

			whiteWeightPlaceholder = tf.placeholder(tf.float32, shape: TotalWeightCount(), name: "whiteWeightPlaceholder");
			whiteBiasPlaceholder = tf.placeholder(tf.float32, shape: TotalBiasCount(), name: "whiteBiasPlaceholder");
			blackWeightPlaceholder = tf.placeholder(tf.float32, shape: TotalWeightCount(), name: "blackWeightPlaceholder");
			blackBiasPlaceholder = tf.placeholder(tf.float32, shape: TotalBiasCount(), name: "blackBiasPlaceholder");

			whiteWeight = tf.Variable(tf.zeros(TotalWeightCount()), name: "whiteWeight");
			whiteBias = tf.Variable(tf.zeros(TotalBiasCount()), name: "whiteBias");
			blackWeight = tf.Variable(tf.zeros(TotalWeightCount()), name: "blackWeight");
			blackBias = tf.Variable(tf.zeros(TotalBiasCount()), name: "blackBias");

			whiteWeightAssignment = whiteWeight.assign(whiteWeightPlaceholder);
			whiteBiasAssignment = whiteBias.assign(whiteBiasPlaceholder);
			blackWeightAssignment = blackWeight.assign(blackWeightPlaceholder);
			blackBiasAssignment = blackBias.assign(blackBiasPlaceholder);

			InitializeNetwork(tf);
		}
		~NeuralPlayerSessionManager()
		{
			session.close();
		}

		public static int TotalWeightCount()
		{
			int count = inputLayerSize * sharedLayerSizes[0];
			for (int layerIndex = 1; layerIndex < sharedLayerSizes.Length; layerIndex++)
			{
				count += sharedLayerSizes[layerIndex - 1] * sharedLayerSizes[layerIndex];
			}
			
			count += sharedLayerSizes[^1] * terminalValueLayerSizes[0];
			for (int layerIndex = 1; layerIndex < terminalValueLayerSizes.Length; layerIndex++)
			{
				count += terminalValueLayerSizes[layerIndex - 1] * terminalValueLayerSizes[layerIndex];
			}
			count += terminalValueLayerSizes[^1] * outputTerminalValueLayerSize;

			count += sharedLayerSizes[^1] * whiteMoveValueLayerSizes[0];
			for (int layerIndex = 1; layerIndex < whiteMoveValueLayerSizes.Length; layerIndex++)
			{
				count += whiteMoveValueLayerSizes[layerIndex - 1] * whiteMoveValueLayerSizes[layerIndex];
			}
			count += whiteMoveValueLayerSizes[^1] * outputWhiteMoveValueLayerSize;

			count += sharedLayerSizes[^1] * blackMoveValueLayerSizes[0];
			for (int layerIndex = 1; layerIndex < blackMoveValueLayerSizes.Length; layerIndex++)
			{
				count += blackMoveValueLayerSizes[layerIndex - 1] * blackMoveValueLayerSizes[layerIndex];
			}
			count += blackMoveValueLayerSizes[^1] * outputBlackMoveValueLayerSize;

			return count;
		}

		public static int TotalBiasCount()
		{
			int count = 0;
			for (int layerIndex = 0; layerIndex < sharedLayerSizes.Length; layerIndex++)
			{
				count += sharedLayerSizes[layerIndex];
			}
			for (int layerIndex = 0; layerIndex < terminalValueLayerSizes.Length; layerIndex++)
			{
				count += terminalValueLayerSizes[layerIndex];
			}
			count += outputTerminalValueLayerSize;
			for (int layerIndex = 0; layerIndex < whiteMoveValueLayerSizes.Length; layerIndex++)
			{
				count += whiteMoveValueLayerSizes[layerIndex];
			}
			count += outputWhiteMoveValueLayerSize;
			for (int layerIndex = 0; layerIndex < blackMoveValueLayerSizes.Length; layerIndex++)
			{
				count += blackMoveValueLayerSizes[layerIndex];
			}
			count += outputBlackMoveValueLayerSize;
			return count;
		}

		private void InitializeNetwork(tensorflow tf)
		{
			input = tf.placeholder(tf.float32, shape: inputLayerSize, name: "I");

			int weightIndex = 0;
			int biasIndex = 0;
			
			(Tensor sharedWhite, Tensor sharedBlack) = BuildLayers(tf, input, input, sharedLayerSizes, ref weightIndex, ref biasIndex);

			(Tensor terminalValueLayerWhite, Tensor terminalValueLayerBlack) = BuildLayers(tf, sharedWhite, sharedBlack, terminalValueLayerSizes, ref weightIndex, ref biasIndex);
			(Tensor whiteMoveValueLayerWhite, Tensor whiteMoveValueLayerBlack) = BuildLayers(tf, sharedWhite, sharedBlack, whiteMoveValueLayerSizes, ref weightIndex, ref biasIndex);
			(Tensor blackMoveValueLayerWhite, Tensor blackMoveValueLayerBlack) = BuildLayers(tf, sharedWhite, sharedBlack, blackMoveValueLayerSizes, ref weightIndex, ref biasIndex);

			(Tensor terminalValueOutputWhite, Tensor terminalValueOutputBlack) = BuildOutput(tf, terminalValueLayerWhite, terminalValueLayerBlack, outputTerminalValueLayerSize, ref weightIndex, ref biasIndex);
			(Tensor whiteMoveValueOutputWhite, Tensor whiteMoveValueOutputBlack) = BuildOutput(tf, whiteMoveValueLayerWhite, whiteMoveValueLayerBlack, outputWhiteMoveValueLayerSize, ref weightIndex, ref biasIndex);
			(Tensor blackMoveValueOutputWhite, Tensor blackMoveValueOutputBlack) = BuildOutput(tf, blackMoveValueLayerWhite, blackMoveValueLayerBlack, outputTerminalValueLayerSize, ref weightIndex, ref biasIndex);

			if (weightIndex != whiteWeight.shape[0])
			{
				Console.WriteLine($"Warning: weightIndex and weight length do not match ({weightIndex} != {whiteWeight.shape[0]})");
			}
			if (biasIndex != whiteBias.shape[0])
			{
				Console.WriteLine($"Warning: biasIndex and bias length do not match ({biasIndex} != {whiteBias.shape[0]})");
			}

			whiteOutput = tf.reshape(tf.stack(new Tensor[] { terminalValueOutputWhite, whiteMoveValueOutputWhite, blackMoveValueOutputWhite }), 3);
			blackOutput = tf.reshape(tf.stack(new Tensor[] { terminalValueOutputBlack, whiteMoveValueOutputBlack, blackMoveValueOutputBlack }), 3);
		}

		private (Tensor, Tensor) BuildLayers(tensorflow tf, Tensor whiteLayer, Tensor blackLayer, int[] layerSizes, ref int weightIndex, ref int biasIndex)
		{
			Tensor lastWhiteLayer = tf.reshape(whiteLayer, (1, whiteLayer.shape[0]));
			Tensor lastBlackLayer = tf.reshape(blackLayer, (1, blackLayer.shape[0]));

			for (int layerIndex = 0; layerIndex < layerSizes.Length; layerIndex++)
			{
				int lastSize = layerIndex == 0 ? whiteLayer.shape[0] : layerSizes[layerIndex - 1];
				int nextSize = layerSizes[layerIndex];

				var layerWhiteWeight = whiteWeight.AsTensor()[new Slice(weightIndex, weightIndex + lastSize * nextSize)];
				layerWhiteWeight = tf.reshape(layerWhiteWeight, (lastSize, nextSize));
				var layerWhiteBias = whiteBias.AsTensor()[new Slice(biasIndex, biasIndex + nextSize)];
				layerWhiteBias = tf.reshape(layerWhiteBias, (1, nextSize));

				var layerBlackWeight = blackWeight.AsTensor()[new Slice(weightIndex, weightIndex + lastSize * nextSize)];
				layerBlackWeight = tf.reshape(layerBlackWeight, (lastSize, nextSize));
				var layerBlackBias = blackBias.AsTensor()[new Slice(biasIndex, biasIndex + nextSize)];
				layerBlackBias = tf.reshape(layerBlackBias, (1, nextSize));

				lastWhiteLayer = tf.nn.relu(tf.matmul(lastWhiteLayer, layerWhiteWeight) + layerWhiteBias);
				lastBlackLayer = tf.nn.relu(tf.matmul(lastBlackLayer, layerBlackWeight) + layerBlackBias);

				weightIndex += lastSize * nextSize;
				biasIndex += nextSize;
			}

			return (tf.reshape(lastWhiteLayer, lastWhiteLayer.shape[1]), tf.reshape(lastBlackLayer, lastBlackLayer.shape[1]));
		}

		private (Tensor, Tensor) BuildOutput(tensorflow tf, Tensor whiteLayer, Tensor blackLayer, int layerSize, ref int weightIndex, ref int biasIndex)
		{
			Tensor lastWhiteLayer = tf.reshape(whiteLayer, (1, whiteLayer.shape[0]));
			Tensor lastBlackLayer = tf.reshape(blackLayer, (1, blackLayer.shape[0]));

			int lastSize = whiteLayer.shape[0];
			int nextSize = layerSize;

			var layerWhiteWeight = whiteWeight.AsTensor()[new Slice(weightIndex, weightIndex + lastSize * nextSize)];
			layerWhiteWeight = tf.reshape(layerWhiteWeight, (lastSize, nextSize));
			var layerWhiteBias = whiteBias.AsTensor()[new Slice(biasIndex, biasIndex + nextSize)];
			layerWhiteBias = tf.reshape(layerWhiteBias, (1, nextSize));

			var layerBlackWeight = blackWeight.AsTensor()[new Slice(weightIndex, weightIndex + lastSize * nextSize)];
			layerBlackWeight = tf.reshape(layerBlackWeight, (lastSize, nextSize));
			var layerBlackBias = blackBias.AsTensor()[new Slice(biasIndex, biasIndex + nextSize)];
			layerBlackBias = tf.reshape(layerBlackBias, (1, nextSize));

			weightIndex += lastSize * nextSize;
			biasIndex += nextSize;

			// normalize outputs to -1, 1
			//var outputWhiteLayer = tf.nn.tanh(tf.matmul(lastWhiteLayer, layerWhiteWeight) + layerWhiteBias);
			//var outputBlackLayer = tf.nn.tanh(tf.matmul(lastBlackLayer, layerBlackWeight) + layerBlackBias);
			var outputWhiteLayer = tf.matmul(lastWhiteLayer, layerWhiteWeight) + layerWhiteBias;
			var outputBlackLayer = tf.matmul(lastBlackLayer, layerBlackWeight) + layerBlackBias;
			return (tf.reshape(outputWhiteLayer, nextSize), tf.reshape(outputBlackLayer, nextSize));
		}

		// Returns 1 if white wins, -1 if black wins, 0 if a stalemate
		// If maxGameMoves is reached and tieBreakWithPieceScore = true, a value will be passed between -1 and 1 depending on the pieces taken
		public float Play(NeuralPlayer whitePlayer, NeuralPlayer blackPlayer, bool tieBreakWithPieceScore = false)
		{
			session.run(
				(whiteWeightAssignment, whiteBiasAssignment, blackWeightAssignment, blackBiasAssignment),
				new FeedItem[]
				{
					new FeedItem(whiteWeightPlaceholder, new NDArray(whitePlayer.weights)),
					new FeedItem(whiteBiasPlaceholder, new NDArray(whitePlayer.biases)),
					new FeedItem(blackWeightPlaceholder, new NDArray(blackPlayer.weights)),
					new FeedItem(blackBiasPlaceholder, new NDArray(blackPlayer.biases)),
				}
			);

			var gameState = GameState.StartPosition();
			
			for (int i = 0; i < maxGameMoveCount; i++)
			{
				//Console.WriteLine(gameState.StateString());
				//Console.ReadKey();

				var nextGameState = PlayMove(gameState);
				if (nextGameState == null)
				{
					// there are no valid further game states; game is over
					if (gameState.IsWhiteInCheck())
					{
						return -1f;
					}
					else if (gameState.IsBlackInCheck())
					{
						return 1f;
					}
					else
					{
						return 0f;
					}
				}
				gameState = nextGameState.Value;
			}
			
			// Too many moves (e.g. infinte loop of moves); stalemate or tie break by piece score
			// TODO: we could possibly detect infinite loops early (or provide the relevant info to the network)
			
			if (tieBreakWithPieceScore)
			{
				// For context the starting value of both sides is 41.52
				// Note that the total value on board can, in rare cases due to promotion, be significantly higher
				float pieceScore = (gameState.WhitePieceScore() - gameState.BlackPieceScore()) / 50f;
				if (pieceScore < -0.9f)
				{
					pieceScore = -0.9f;
				}
				if (pieceScore > 0.9f)
				{
					pieceScore = 0.9f;
				}
				return pieceScore;
			}
			else
			{
				return 0;
			}
		}

		private unsafe GameState? PlayMove(GameState gameState)
		{
			Tile playerColor = gameState.move;
			Tensor playerOutput = playerColor == Tile.White ? whiteOutput : blackOutput;

			// TODO: when adding to unevaluatedNodes and another entry has the exact same score, it is likely the same state (reached by a different sequence of moves)
			//       in this case this is plenty of opportunity for optimization

			var nodes = new GameStateNode[maxMoveEvaluationsCount + 256];
			int nodeIndex = 0;
			int rootNodeCount = 0;

			// The key is of type (float, int) with the int being a value unique to the node to make all keys unique but ordered by the first value
			var unevaluatedNodes = new SortedList<(float, int), int>(maxMoveEvaluationsCount + 256);

			var rootGameStates = gameState.NextGameStates();
			if (rootGameStates.Count == 0)
			{
				return null;
			}
			rootNodeCount = rootGameStates.Count;

			int remainingEvaluations = maxMoveEvaluationsCount;
			foreach (var rootState in rootGameStates)
			{
				(float terminalValue, float whiteMoveValue, float blackMoveValue) = EvaluateBoard(rootState, gameState, 1, playerOutput);
				remainingEvaluations--;
				var moveValue = playerColor == Tile.White ? whiteMoveValue : blackMoveValue;

				var node = new GameStateNode
				{
					state = rootState,
					rootMoveIndex = nodeIndex,
					depth = 1,
					terminalValue = terminalValue,
					moveValue = moveValue,
					parentIndex = -1,
					childrenCount = -1,
				};

				nodes[nodeIndex] = node;
				if (moveValue > 0)
				{
					// negative self move value to sort descending; remainingEvaluations is unique
					unevaluatedNodes.Add((-moveValue, remainingEvaluations), nodeIndex);
				}
				nodeIndex++;
			}

			while (remainingEvaluations > 0 && unevaluatedNodes.Count > 0)
			{
				// node with highest move value
				int currentNodeIndex = unevaluatedNodes.Values[0];
				var currentNode = nodes[currentNodeIndex];
				unevaluatedNodes.RemoveAt(0);

				var nextStates = currentNode.state.NextGameStates();
				if (nextStates.Count == 0)
				{
					// Game has finished; we can set the terminal value directly
					if (currentNode.state.IsWhiteInCheck())
					{
						currentNode.terminalValue = float.NegativeInfinity;
					}
					else if (currentNode.state.IsBlackInCheck())
					{
						currentNode.terminalValue = float.PositiveInfinity;
					}
					else
					{
						currentNode.terminalValue = 0;
					}
					continue;
				}

				currentNode.childrenCount = nextStates.Count;
				for (int nextStateIndex = 0; nextStateIndex < nextStates.Count; nextStateIndex++)
				{
					var nextState = nextStates[nextStateIndex];
					(float terminalValue, float whiteMoveValue, float blackMoveValue) = EvaluateBoard(nextState, currentNode.state, currentNode.depth + 1, playerOutput);
					remainingEvaluations--;
					var moveValue = currentNode.state.move == Tile.White ? whiteMoveValue : blackMoveValue;
					
					var node = new GameStateNode
					{
						state = nextState,
						rootMoveIndex = currentNode.rootMoveIndex,
						depth = currentNode.depth + 1,
						terminalValue = terminalValue,
						moveValue = moveValue,
						parentIndex = currentNodeIndex,
						childrenCount = -1,
					};

					nodes[nodeIndex] = node;
					if (moveValue > 0)
					{
						// negative moveValue to sort descending; remainingEvaluations is unique
						unevaluatedNodes.Add((-moveValue, remainingEvaluations), nodeIndex);
					}
					currentNode.childrenIndices[nextStateIndex] = nodeIndex;
					nodeIndex++;
				}
			}

			float ScoreNode(GameStateNode node)
			{
				if (node.childrenCount <= 0)
				{
					return node.terminalValue;
				}
				if (node.state.move == Tile.White)
				{
					// maximize over white's moves
					float highestScore = float.NegativeInfinity;
					for (int childIndex = 0; childIndex < node.childrenCount; childIndex++)
					{
						var child = nodes[node.childrenIndices[childIndex]];
						float childScore = ScoreNode(child);
						if (childScore > highestScore)
						{
							highestScore = childScore;
						}
					}
					return highestScore;
				}
				else
				{
					// minimize over black's moves
					float lowestScore = float.PositiveInfinity;
					for (int childIndex = 0; childIndex < node.childrenCount; childIndex++)
					{
						var child = nodes[node.childrenIndices[childIndex]];
						float childScore = ScoreNode(child);
						if (childScore < lowestScore)
						{
							lowestScore = childScore;
						}
					}
					return lowestScore;
				}
			}

			// Traverse tree with minimax
			// TODO: we can be intelligent about collapsing nodes as they are evaluated rather than waiting until the end
			int highestRootIndex = 0;
			float highestRootScore = float.NegativeInfinity;
			for (int rootNodeIndex = 0; rootNodeIndex < rootNodeCount; rootNodeIndex++)
			{
				float score = ScoreNode(nodes[rootNodeIndex]);
				if (score > highestRootScore)
				{
					highestRootScore = score;
					highestRootIndex = rootNodeIndex;
				}
			}

			// Console.WriteLine($"Remaining evaluations = {remainingEvaluations}");

			return nodes[highestRootIndex].state;
		}

		private (float, float, float) EvaluateBoard(GameState gameState, GameState previousGameState, int depth, Tensor output)
		{
			var networkInput = GameStateToNetworkInput(gameState, previousGameState, depth);
			var result = session.run(output, new FeedItem(input, networkInput));
			float terminalValue = result[0];
			float whiteMoveValue = result[1];
			float blackMoveValue = result[2];
			return (terminalValue, whiteMoveValue, blackMoveValue);
		}

		private float[] GameStateToNetworkInput(GameState gameState, GameState previousGameState, float depth)
		{
			var result = new float[inputLayerSize];
			for (int s = 0; s < 2; s++)
			{
				for (int y = 0; y < 8; y++)
				{
					for (int x = 0; x < 8; x++)
					{
						var state = s == 0 ? gameState : previousGameState;
						switch (state.GetTile(x, y))
						{
							case Tile.Empty:       result[((s * 8 + y) * 8 + x) * 13 +  0] = 1f; break;
							case Tile.WhitePawn:   result[((s * 8 + y) * 8 + x) * 13 +  1] = 1f; break;
							case Tile.WhiteRook:   result[((s * 8 + y) * 8 + x) * 13 +  2] = 1f; break;
							case Tile.WhiteKnight: result[((s * 8 + y) * 8 + x) * 13 +  3] = 1f; break;
							case Tile.WhiteBishop: result[((s * 8 + y) * 8 + x) * 13 +  4] = 1f; break;
							case Tile.WhiteQueen:  result[((s * 8 + y) * 8 + x) * 13 +  5] = 1f; break;
							case Tile.WhiteKing:   result[((s * 8 + y) * 8 + x) * 13 +  6] = 1f; break;
							case Tile.BlackPawn:   result[((s * 8 + y) * 8 + x) * 13 +  7] = 1f; break;
							case Tile.BlackRook:   result[((s * 8 + y) * 8 + x) * 13 +  8] = 1f; break;
							case Tile.BlackKnight: result[((s * 8 + y) * 8 + x) * 13 +  9] = 1f; break;
							case Tile.BlackBishop: result[((s * 8 + y) * 8 + x) * 13 + 10] = 1f; break;
							case Tile.BlackQueen:  result[((s * 8 + y) * 8 + x) * 13 + 11] = 1f; break;
							case Tile.BlackKing:   result[((s * 8 + y) * 8 + x) * 13 + 12] = 1f; break;
						}
					}
				}
			}
			result[2 * 64 * 13] = depth;
			return result;
		}
	}

	unsafe struct GameStateNode
	{
		public GameState state;
		public int rootMoveIndex;
		public int depth;
		public float terminalValue;
		public float moveValue;
		public int parentIndex;
		public fixed int childrenIndices[256];
		public int childrenCount;
	}

	public class NeuralPlayer : Player
	{
		public float[] weights;
		public float[] biases;

		public NeuralPlayer(float[] weights, float[] biases)
		{
			this.weights = weights;
			this.biases = biases;
		}

		public NeuralPlayer(int seed)
		{
			var random = new Random(seed);
			weights = new float[NeuralPlayerSessionManager.TotalWeightCount()];
			biases = new float[NeuralPlayerSessionManager.TotalBiasCount()];

			for (int weightIndex = 0; weightIndex < weights.Length; weightIndex++)
			{
				weights[weightIndex] = (float)(random.NextDouble() * 2 - 1);
			}
			for (int biasIndex = 0; biasIndex < biases.Length; biasIndex++)
			{
				biases[biasIndex] = (float)(random.NextDouble() * 2 - 1);
			}
		}

		public int Move(GameState currentState, List<GameState> nextStates)
		{
			return 0;
		}

		public void Save(string filename)
		{
			using (var writer = new BinaryWriter(File.Open(filename, FileMode.Create)))
			{
				writer.Write(weights.Length);
				writer.Write(biases.Length);
				foreach (var weight in weights)
				{
					writer.Write(weight);
				}
				foreach (var bias in weights)
				{
					writer.Write(bias);
				}
			}
		}

		public static NeuralPlayer Load(string filename)
		{
			float[] weights;
			float[] biases;

			using (var reader = new BinaryReader(File.OpenRead(filename)))
			{
				int weightsLength = reader.ReadInt32();
				int biasesLength = reader.ReadInt32();
				weights = new float[weightsLength];
				biases = new float[biasesLength];
				for (int weightIndex = 0; weightIndex < weightsLength; weightIndex++)
				{
					weights[weightIndex] = reader.ReadSingle();
				}
				for (int biasIndex = 0; biasIndex < biasesLength; biasIndex++)
				{
					biases[biasIndex] = reader.ReadSingle();
				}
			}

			return new NeuralPlayer(weights, biases);
		}
	}
}
