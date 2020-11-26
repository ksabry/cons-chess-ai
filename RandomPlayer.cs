using System;
using System.Collections.Generic;
using System.Text;

namespace Chess
{
	class RandomPlayer : Player
	{
		private Random random;
		public RandomPlayer()
		{
			random = new Random();
		}

		public int Move(GameState currentState, List<GameState> nextStates)
		{
			return random.Next(nextStates.Count);
		}
	}
}
