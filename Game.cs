using System;
using System.Collections.Generic;
using System.Text;

namespace Chess
{
	public class Game
	{
		private GameState state;
		private readonly Player whitePlayer;
		private readonly Player blackPlayer;

		public Game(Player whitePlayer, Player blackPlayer)
		{
			this.whitePlayer = whitePlayer;
			this.blackPlayer = blackPlayer;
		}

		public Tile Play()
		{
			state = GameState.StartPosition();
			for (int i = 0; i < 100; i++)
			{
				var nextStates = state.NextGameStates();
				if (nextStates.Count == 0)
				{
					if (state.IsWhiteInCheck())
					{
						return Tile.Black;
					}
					else if (state.IsBlackInCheck())
					{
						return Tile.White;
					}
					else
					{
						return Tile.Empty;
					}
				}
				var moveIndex = whitePlayer.Move(state, nextStates);
				state = nextStates[moveIndex];

				Console.WriteLine(state.StateString());
				if (state.IsBlackInCheck())
				{
					Console.WriteLine("Black in check");
				}
				Console.ReadKey();

				nextStates = state.NextGameStates();
				if (nextStates.Count == 0)
				{
					if (state.IsWhiteInCheck())
					{
						return Tile.Black;
					}
					else if (state.IsBlackInCheck())
					{
						return Tile.White;
					}
					else
					{
						return Tile.Empty;
					}
				}
				moveIndex = blackPlayer.Move(state, nextStates);
				state = nextStates[moveIndex];

				Console.WriteLine(state.StateString());
				if (state.IsWhiteInCheck())
				{
					Console.WriteLine("White in check");
				}
				Console.ReadKey();
			}
			return Tile.Empty;
		}
	}
}
