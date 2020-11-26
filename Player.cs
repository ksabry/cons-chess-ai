using System;
using System.Collections.Generic;
using System.Text;

namespace Chess
{
	public interface Player
	{
		int Move(GameState currentState, List<GameState> nextStates);
	}
}
