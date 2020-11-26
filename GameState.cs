using System;
using System.Collections.Generic;
using System.Text;

namespace Chess
{
	public enum Tile : uint
	{
		Empty = 0,
		
		Pawn   = 1 << 0,
		Rook   = 1 << 1,
		Knight = 1 << 2,
		Bishop = 1 << 3,
		Queen  = 1 << 4,
		King   = 1 << 6,

		Piece = Pawn | Rook | Knight | Bishop | Queen | King,

		White  = 1 << 7,
		Black  = 1 << 8,
		
		Color = White | Black,

		WhitePawn   = White | Pawn,
		WhiteRook   = White | Rook,
		WhiteKnight = White | Knight,
		WhiteBishop = White | Bishop,
		WhiteQueen  = White | Queen,
		WhiteKing   = White | King,

		BlackPawn   = Black | Pawn,
		BlackRook   = Black | Rook,
		BlackKnight = Black | Knight,
		BlackBishop = Black | Bishop,
		BlackQueen  = Black | Queen,
		BlackKing   = Black | King,
	}

	public enum CastleMove : uint
	{
		WhiteKingSide = 1 << 0,
		WhiteQueenSide = 1 << 1,
		BlackKingSide = 1 << 2,
		BlackQueenSide = 1 << 3,
	}

	public unsafe struct GameState
	{
		private fixed uint board[64];
		public CastleMove castlesAvailable;
		public int whiteEnPassantPawn;
		public int blackEnPassantPawn;
		public readonly Tile move; // Either Tile.White or Tile.Black

		private int whiteKingX;
		private int whiteKingY;
		private int blackKingX;
		private int blackKingY;

		public static GameState StartPosition()
		{
			uint[] board = new uint[]
			{
				(uint)Tile.WhiteRook  , (uint)Tile.WhitePawn, (uint)Tile.Empty, (uint)Tile.Empty, (uint)Tile.Empty, (uint)Tile.Empty, (uint)Tile.BlackPawn, (uint)Tile.BlackRook  ,
				(uint)Tile.WhiteKnight, (uint)Tile.WhitePawn, (uint)Tile.Empty, (uint)Tile.Empty, (uint)Tile.Empty, (uint)Tile.Empty, (uint)Tile.BlackPawn, (uint)Tile.BlackKnight,
				(uint)Tile.WhiteBishop, (uint)Tile.WhitePawn, (uint)Tile.Empty, (uint)Tile.Empty, (uint)Tile.Empty, (uint)Tile.Empty, (uint)Tile.BlackPawn, (uint)Tile.BlackBishop,
				(uint)Tile.WhiteQueen , (uint)Tile.WhitePawn, (uint)Tile.Empty, (uint)Tile.Empty, (uint)Tile.Empty, (uint)Tile.Empty, (uint)Tile.BlackPawn, (uint)Tile.BlackQueen ,
				(uint)Tile.WhiteKing  , (uint)Tile.WhitePawn, (uint)Tile.Empty, (uint)Tile.Empty, (uint)Tile.Empty, (uint)Tile.Empty, (uint)Tile.BlackPawn, (uint)Tile.BlackKing  ,
				(uint)Tile.WhiteBishop, (uint)Tile.WhitePawn, (uint)Tile.Empty, (uint)Tile.Empty, (uint)Tile.Empty, (uint)Tile.Empty, (uint)Tile.BlackPawn, (uint)Tile.BlackBishop,
				(uint)Tile.WhiteKnight, (uint)Tile.WhitePawn, (uint)Tile.Empty, (uint)Tile.Empty, (uint)Tile.Empty, (uint)Tile.Empty, (uint)Tile.BlackPawn, (uint)Tile.BlackKnight,
				(uint)Tile.WhiteRook  , (uint)Tile.WhitePawn, (uint)Tile.Empty, (uint)Tile.Empty, (uint)Tile.Empty, (uint)Tile.Empty, (uint)Tile.BlackPawn, (uint)Tile.BlackRook  ,
			};

			var move = Tile.White;
			var castlesAvailable = CastleMove.BlackQueenSide | CastleMove.BlackKingSide | CastleMove.WhiteQueenSide | CastleMove.WhiteKingSide;
			var whiteEnPassantPawn = 9;
			var blackEnPassantPawn = 9;

			var whiteKingX = 4;
			var whiteKingY = 0;
			var blackKingX = 4;
			var blackKingY = 7;

			fixed (uint* boardToCopy = board)
			{
				return new GameState(boardToCopy, move, castlesAvailable, whiteEnPassantPawn, blackEnPassantPawn, whiteKingX, whiteKingY, blackKingX, blackKingY);
			}
		}

		private GameState(uint* board, Tile move, CastleMove castlesAvailable, int whiteEnPassantPawn, int blackEnPassantPawn, int whiteKingX, int whiteKingY, int blackKingX, int blackKingY)
		{
			for (int i = 0; i < 64; i++)
			{
				this.board[i] = board[i];
			}
			this.move = move;
			this.castlesAvailable = castlesAvailable;
			this.whiteEnPassantPawn = whiteEnPassantPawn;
			this.blackEnPassantPawn = blackEnPassantPawn;
			this.whiteKingX = whiteKingX;
			this.whiteKingY = whiteKingY;
			this.blackKingX = blackKingX;
			this.blackKingY = blackKingY;
		}

		public Tile GetTile(int x, int y)
		{
			return (Tile)board[x * 8 + y];
		}

		public void SetTile(int x, int y, Tile tile)
		{
			board[x * 8 + y] = (uint)tile;
		}

		private GameState CloneForNextMove()
		{
			fixed (uint* boardToCopy = board)
			{
				return new GameState(boardToCopy, Tile.Color & (~move), castlesAvailable, 9, 9, whiteKingX, whiteKingY, blackKingX, blackKingY);
			}
		}

		private GameState Clone()
		{
			fixed (uint* boardToCopy = board)
			{
				return new GameState(boardToCopy, move, castlesAvailable, whiteEnPassantPawn, blackEnPassantPawn, whiteKingX, whiteKingY, blackKingX, blackKingY);
			}
		}
		
		public float WhitePieceScore()
		{
			float total = 0;
			for (int y = 0; y < 8; y++)
			{
				for (int x = 0; x < 8; x++)
				{
					var tile = GetTile(x, y);
					switch (tile)
					{
						// Values from an AlphaZero paper
						case Tile.WhitePawn:
							total += 1f;
							break;
						case Tile.WhiteRook:
							total += 5.63f;
							break;
						case Tile.WhiteKnight:
							total += 3.05f;
							break;
						case Tile.WhiteBishop:
							total += 3.33f;
							break;
						case Tile.WhiteQueen:
							total += 9.5f;
							break;
					}
				}
			}
			return total;
		}
		public float BlackPieceScore()
		{
			float total = 0;
			for (int y = 0; y < 8; y++)
			{
				for (int x = 0; x < 8; x++)
				{
					var tile = GetTile(x, y);
					switch (tile)
					{
						// Values from an AlphaZero paper
						case Tile.BlackPawn:
							total += 1f;
							break;
						case Tile.BlackRook:
							total += 5.63f;
							break;
						case Tile.BlackKnight:
							total += 3.05f;
							break;
						case Tile.BlackBishop:
							total += 3.33f;
							break;
						case Tile.BlackQueen:
							total += 9.5f;
							break;
					}
				}
			}
			return total;
		}

		public bool IsWhiteInCheck()
		{
			int tx, ty;

			// Check cardinal directions
			for (tx = whiteKingX - 1; tx >= 0; tx--)
			{
				Tile tile = GetTile(tx, whiteKingY);
				if (tile == Tile.BlackRook || tile == Tile.BlackQueen)
				{
					return true;
				}
				else if (tile != Tile.Empty)
				{
					break;
				}
			}
			for (tx = whiteKingX + 1; tx < 8; tx++)
			{
				Tile tile = GetTile(tx, whiteKingY);
				if (tile == Tile.BlackRook || tile == Tile.BlackQueen)
				{
					return true;
				}
				else if (tile != Tile.Empty)
				{
					break;
				}
			}
			for (ty = whiteKingY - 1; ty >= 0; ty--)
			{
				Tile tile = GetTile(whiteKingX, ty);
				if (tile == Tile.BlackRook || tile == Tile.BlackQueen)
				{
					return true;
				}
				else if (tile != Tile.Empty)
				{
					break;
				}
			}
			for (ty = whiteKingY + 1; ty < 8; ty++)
			{
				Tile tile = GetTile(whiteKingX, ty);
				if (tile == Tile.BlackRook || tile == Tile.BlackQueen)
				{
					return true;
				}
				else if (tile != Tile.Empty)
				{
					break;
				}
			}

			// Check diagonals
			tx = whiteKingX - 1;
			ty = whiteKingY - 1;
			while (tx >= 0 && ty >= 0)
			{
				Tile tile = GetTile(tx, ty);
				if (tile == Tile.BlackBishop || tile == Tile.BlackQueen)
				{
					return true;
				}
				else if (tile != Tile.Empty)
				{
					break;
				}
				tx--;
				ty--;
			}
			tx = whiteKingX - 1;
			ty = whiteKingY + 1;
			while (tx >= 0 && ty < 8)
			{
				Tile tile = GetTile(tx, ty);
				if (tile == Tile.BlackBishop || tile == Tile.BlackQueen)
				{
					return true;
				}
				else if (tile != Tile.Empty)
				{
					break;
				}
				tx--;
				ty++;
			}
			tx = whiteKingX + 1;
			ty = whiteKingY - 1;
			while (tx < 8 && ty >= 0)
			{
				Tile tile = GetTile(tx, ty);
				if (tile == Tile.BlackBishop || tile == Tile.BlackQueen)
				{
					return true;
				}
				else if (tile != Tile.Empty)
				{
					break;
				}
				tx++;
				ty--;
			}
			tx = whiteKingX + 1;
			ty = whiteKingY + 1;
			while (tx < 8 && ty < 8)
			{
				Tile tile = GetTile(tx, ty);
				if (tile == Tile.BlackBishop || tile == Tile.BlackQueen)
				{
					return true;
				}
				else if (tile != Tile.Empty)
				{
					break;
				}
				tx++;
				ty++;
			}

			// Check knights
			if (whiteKingX >= 2)
			{
				if (
					whiteKingY >= 1 && GetTile(whiteKingX - 2, whiteKingY - 1) == Tile.BlackKnight ||
					whiteKingY <= 6 && GetTile(whiteKingX - 2, whiteKingY + 1) == Tile.BlackKnight
				)
				{
					return true;
				}
			}
			if (whiteKingX >= 1)
			{
				if (
					whiteKingY >= 2 && GetTile(whiteKingX - 1, whiteKingY - 2) == Tile.BlackKnight ||
					whiteKingY <= 5 && GetTile(whiteKingX - 1, whiteKingY + 2) == Tile.BlackKnight
				)
				{
					return true;
				}
			}
			if (whiteKingX <= 5)
			{
				if (
					whiteKingY >= 1 && GetTile(whiteKingX + 2, whiteKingY - 1) == Tile.BlackKnight ||
					whiteKingY <= 6 && GetTile(whiteKingX + 2, whiteKingY + 1) == Tile.BlackKnight
				)
				{
					return true;
				}
			}
			if (whiteKingX <= 6)
			{
				if (
					whiteKingY >= 2 && GetTile(whiteKingX + 1, whiteKingY - 2) == Tile.BlackKnight ||
					whiteKingY <= 5 && GetTile(whiteKingX + 1, whiteKingY + 2) == Tile.BlackKnight
				)
				{
					return true;
				}
			}

			// Check kings and pawns
			// Note that while two kings next to each other is an invalid position, the code which checks this very validity relies on this method so this is necessary to implement
			if (
				(
					whiteKingY >= 1 && GetTile(whiteKingX, whiteKingY - 1) == Tile.BlackKing ||
					whiteKingY <= 6 && GetTile(whiteKingX, whiteKingY + 1) == Tile.BlackKing
				) ||
				(
					whiteKingX >= 1 &&
					(
						GetTile(whiteKingX - 1, whiteKingY) == Tile.BlackKing ||
						whiteKingY >= 1 && GetTile(whiteKingX - 1, whiteKingY - 1) == Tile.BlackKing ||
						whiteKingY <= 6 && (GetTile(whiteKingX - 1, whiteKingY + 1) == Tile.BlackKing || GetTile(whiteKingX - 1, whiteKingY + 1) == Tile.BlackPawn)
					)
				) ||
				(
					whiteKingX <= 6 &&
					(
						GetTile(whiteKingX + 1, whiteKingY) == Tile.BlackKing ||
						whiteKingY >= 1 && GetTile(whiteKingX + 1, whiteKingY - 1) == Tile.BlackKing ||
						whiteKingY <= 6 && (GetTile(whiteKingX + 1, whiteKingY + 1) == Tile.BlackKing || GetTile(whiteKingX + 1, whiteKingY + 1) == Tile.BlackPawn)
					)
				)
			)
			{
				return true;
			}

			return false;
		}

		public bool IsBlackInCheck()
		{
			int tx, ty;

			// Check cardinal directions
			for (tx = blackKingX - 1; tx >= 0; tx--)
			{
				Tile tile = GetTile(tx, blackKingY);
				if (tile == Tile.WhiteRook || tile == Tile.WhiteQueen)
				{
					return true;
				}
				else if (tile != Tile.Empty)
				{
					break;
				}
			}
			for (tx = blackKingX + 1; tx < 8; tx++)
			{
				Tile tile = GetTile(tx, blackKingY);
				if (tile == Tile.WhiteRook || tile == Tile.WhiteQueen)
				{
					return true;
				}
				else if (tile != Tile.Empty)
				{
					break;
				}
			}
			for (ty = blackKingY - 1; ty >= 0; ty--)
			{
				Tile tile = GetTile(blackKingX, ty);
				if (tile == Tile.WhiteRook || tile == Tile.WhiteQueen)
				{
					return true;
				}
				else if (tile != Tile.Empty)
				{
					break;
				}
			}
			for (ty = blackKingY + 1; ty < 8; ty++)
			{
				Tile tile = GetTile(blackKingX, ty);
				if (tile == Tile.WhiteRook || tile == Tile.WhiteQueen)
				{
					return true;
				}
				else if (tile != Tile.Empty)
				{
					break;
				}
			}

			// Check diagonals
			tx = blackKingX - 1;
			ty = blackKingY - 1;
			while (tx >= 0 && ty >= 0)
			{
				Tile tile = GetTile(tx, ty);
				if (tile == Tile.WhiteBishop || tile == Tile.WhiteQueen)
				{
					return true;
				}
				else if (tile != Tile.Empty)
				{
					break;
				}
				tx--;
				ty--;
			}
			tx = blackKingX - 1;
			ty = blackKingY + 1;
			while (tx >= 0 && ty < 8)
			{
				Tile tile = GetTile(tx, ty);
				if (tile == Tile.WhiteBishop || tile == Tile.WhiteQueen)
				{
					return true;
				}
				else if (tile != Tile.Empty)
				{
					break;
				}
				tx--;
				ty++;
			}
			tx = blackKingX + 1;
			ty = blackKingY - 1;
			while (tx < 8 && ty >= 0)
			{
				Tile tile = GetTile(tx, ty);
				if (tile == Tile.WhiteBishop || tile == Tile.WhiteQueen)
				{
					return true;
				}
				else if (tile != Tile.Empty)
				{
					break;
				}
				tx++;
				ty--;
			}
			tx = blackKingX + 1;
			ty = blackKingY + 1;
			while (tx < 8 && ty < 8)
			{
				Tile tile = GetTile(tx, ty);
				if (tile == Tile.WhiteBishop || tile == Tile.WhiteQueen)
				{
					return true;
				}
				else if (tile != Tile.Empty)
				{
					break;
				}
				tx++;
				ty++;
			}

			// Check knights
			if (blackKingX >= 2)
			{
				if (
					blackKingY >= 1 && GetTile(blackKingX - 2, blackKingY - 1) == Tile.WhiteKnight ||
					blackKingY <= 6 && GetTile(blackKingX - 2, blackKingY + 1) == Tile.WhiteKnight
				)
				{
					return true;
				}
			}
			if (blackKingX >= 1)
			{
				if (
					blackKingY >= 2 && GetTile(blackKingX - 1, blackKingY - 2) == Tile.WhiteKnight ||
					blackKingY <= 5 && GetTile(blackKingX - 1, blackKingY + 2) == Tile.WhiteKnight
				)
				{
					return true;
				}
			}
			if (blackKingX <= 5)
			{
				if (
					blackKingY >= 1 && GetTile(blackKingX + 2, blackKingY - 1) == Tile.WhiteKnight ||
					blackKingY <= 6 && GetTile(blackKingX + 2, blackKingY + 1) == Tile.WhiteKnight
				)
				{
					return true;
				}
			}
			if (blackKingX <= 6)
			{
				if (
					blackKingY >= 2 && GetTile(blackKingX + 1, blackKingY - 2) == Tile.WhiteKnight ||
					blackKingY <= 5 && GetTile(blackKingX + 1, blackKingY + 2) == Tile.WhiteKnight
				)
				{
					return true;
				}
			}

			// Check kings and pawns
			// Note that while two kings next to each other is an invalid position, the code which checks this very validity relies on this method so this is necessary to implement
			if (
				(
					blackKingY >= 1 && GetTile(blackKingX, blackKingY - 1) == Tile.WhiteKing ||
					blackKingY <= 6 && GetTile(blackKingX, blackKingY + 1) == Tile.WhiteKing
				) ||
				(
					blackKingX >= 1 &&
					(
						GetTile(blackKingX - 1, blackKingY) == Tile.WhiteKing ||
						blackKingY >= 1 && (GetTile(blackKingX - 1, blackKingY - 1) == Tile.WhiteKing || GetTile(blackKingX - 1, blackKingY - 1) == Tile.WhitePawn) ||
						blackKingY <= 6 && GetTile(blackKingX - 1, blackKingY + 1) == Tile.WhiteKing
					)
				) ||
				(
					blackKingX <= 6 &&
					(
						GetTile(blackKingX + 1, blackKingY) == Tile.WhiteKing ||
						blackKingY >= 1 && (GetTile(blackKingX + 1, blackKingY - 1) == Tile.WhiteKing || GetTile(blackKingX + 1, blackKingY - 1) == Tile.WhitePawn) ||
						blackKingY <= 6 && GetTile(blackKingX + 1, blackKingY + 1) == Tile.WhiteKing
					)
				)
			)
			{
				return true;
			}

			return false;
		}

		public string BoardString()
		{
			string result = "";
			for (int y = 7; y >= 0; y--)
			{
				for (int x = 0; x < 8; x++)
				{
					switch (GetTile(x, y))
					{
						case Tile.Empty:       result += "-- "; break;
						case Tile.WhitePawn:   result += "WP "; break;
						case Tile.WhiteRook:   result += "WR "; break;
						case Tile.WhiteKnight: result += "WN "; break;
						case Tile.WhiteBishop: result += "WB "; break;
						case Tile.WhiteQueen:  result += "WQ "; break;
						case Tile.WhiteKing:   result += "WK "; break;
						case Tile.BlackPawn:   result += "BP "; break;
						case Tile.BlackRook:   result += "BR "; break;
						case Tile.BlackKnight: result += "BN "; break;
						case Tile.BlackBishop: result += "BB "; break;
						case Tile.BlackQueen:  result += "BQ "; break;
						case Tile.BlackKing:   result += "BK "; break;
					}
				}
				result += "\n";
			}
			return result;
		}

		public string StateString()
		{
			string result = BoardString();
			result += "Castles Available: ";
			if ((castlesAvailable & CastleMove.WhiteKingSide) != 0)
			{
				result += "WK ";
			}
			if ((castlesAvailable & CastleMove.WhiteQueenSide) != 0)
			{
				result += "WQ ";
			}
			if ((castlesAvailable & CastleMove.BlackKingSide) != 0)
			{
				result += "BK ";
			}
			if ((castlesAvailable & CastleMove.BlackQueenSide) != 0)
			{
				result += "BQ ";
			}
			result += "\n";
			if (whiteEnPassantPawn < 9)
			{
				result += "White En Passant Column: " + whiteEnPassantPawn + "\n";
			}
			if (blackEnPassantPawn < 9)
			{
				result += "Black En Passant Column: " + blackEnPassantPawn + "\n";
			}
			return result;
		}

		public List<GameState> NextGameStates()
		{
			var result = new List<GameState>(64);
			GameState nextState;
			int x, y, tx, ty;
			CastleMove castlesAvailableModifier;

			if (move == Tile.White)
			{
				for (x = 0; x < 8; x++)
				{
					for (y = 0; y < 8; y++)
					{
						Tile tile = GetTile(x, y);
						castlesAvailableModifier = CastleMove.WhiteKingSide | CastleMove.WhiteQueenSide | CastleMove.BlackKingSide | CastleMove.BlackQueenSide;
						switch (tile)
						{
							case Tile.WhitePawn:
								if (GetTile(x, y + 1) == Tile.Empty)
								{
									if (y == 6)
									{
										// White Pawn Promotion
										nextState = CloneForNextMove();
										nextState.SetTile(x, 6, Tile.Empty);
										nextState.SetTile(x, 7, Tile.WhiteQueen);
										if (!nextState.IsWhiteInCheck())
										{
											result.Add(nextState);
										}

										nextState = CloneForNextMove();
										nextState.SetTile(x, 6, Tile.Empty);
										nextState.SetTile(x, 7, Tile.WhiteKnight);
										if (!nextState.IsWhiteInCheck())
										{
											result.Add(nextState);
										}

										nextState = CloneForNextMove();
										nextState.SetTile(x, 6, Tile.Empty);
										nextState.SetTile(x, 7, Tile.WhiteBishop);
										if (!nextState.IsWhiteInCheck())
										{
											result.Add(nextState);
										}

										nextState = CloneForNextMove();
										nextState.SetTile(x, 6, Tile.Empty);
										nextState.SetTile(x, 7, Tile.WhiteRook);
										if (!nextState.IsWhiteInCheck())
										{
											result.Add(nextState);
										}
									}
									else
									{
										// White Pawn Forward One
										nextState = CloneForNextMove();
										nextState.SetTile(x, y, Tile.Empty);
										nextState.SetTile(x, y + 1, Tile.WhitePawn);
										if (!nextState.IsWhiteInCheck())
										{
											result.Add(nextState);
										}

										if (y == 1 && GetTile(x, 3) == Tile.Empty)
										{
											// White Pawn Forward Two
											nextState = CloneForNextMove();
											nextState.SetTile(x, 1, Tile.Empty);
											nextState.SetTile(x, 3, Tile.WhitePawn);
											if (!nextState.IsWhiteInCheck())
											{
												nextState.whiteEnPassantPawn = x;
												result.Add(nextState);
											}
										}
									}
								}
								if (x != 0 && (GetTile(x - 1, y + 1) & Tile.Black) > 0)
								{
									if (y == 6)
									{
										// White Pawn Taking Left Promotion
										nextState = CloneForNextMove();
										nextState.SetTile(x, 6, Tile.Empty);
										nextState.SetTile(x - 1, 7, Tile.WhiteQueen);
										if (!nextState.IsWhiteInCheck())
										{
											result.Add(nextState);
										}

										result.Add(nextState);
										nextState = CloneForNextMove();
										nextState.SetTile(x, 6, Tile.Empty);
										nextState.SetTile(x - 1, 7, Tile.WhiteKnight);
										if (!nextState.IsWhiteInCheck())
										{
											result.Add(nextState);
										}

										nextState = CloneForNextMove();
										nextState.SetTile(x, 6, Tile.Empty);
										nextState.SetTile(x - 1, 7, Tile.WhiteBishop);
										if (!nextState.IsWhiteInCheck())
										{
											result.Add(nextState);
										}

										nextState = CloneForNextMove();
										nextState.SetTile(x, 6, Tile.Empty);
										nextState.SetTile(x - 1, 7, Tile.WhiteRook);
										if (!nextState.IsWhiteInCheck())
										{
											result.Add(nextState);
										}
									}
									else
									{
										// White Pawn Taking Left
										nextState = CloneForNextMove();
										nextState.SetTile(x, y, Tile.Empty);
										nextState.SetTile(x - 1, y + 1, Tile.WhitePawn);
										if (!nextState.IsWhiteInCheck())
										{
											result.Add(nextState);
										}
									}
								}
								if (x != 7 && (GetTile(x + 1, y + 1) & Tile.Black) > 0)
								{
									if (y == 6)
									{
										// White Pawn Taking Right Promotion
										nextState = CloneForNextMove();
										nextState.SetTile(x, 6, Tile.Empty);
										nextState.SetTile(x + 1, 7, Tile.WhiteQueen);
										if (!nextState.IsWhiteInCheck())
										{
											result.Add(nextState);
										}

										nextState = CloneForNextMove();
										nextState.SetTile(x, 6, Tile.Empty);
										nextState.SetTile(x + 1, 7, Tile.WhiteKnight);
										if (!nextState.IsWhiteInCheck())
										{
											result.Add(nextState);
										}

										nextState = CloneForNextMove();
										nextState.SetTile(x, 6, Tile.Empty);
										nextState.SetTile(x + 1, 7, Tile.WhiteBishop);
										if (!nextState.IsWhiteInCheck())
										{
											result.Add(nextState);
										}

										nextState = CloneForNextMove();
										nextState.SetTile(x, 6, Tile.Empty);
										nextState.SetTile(x + 1, 7, Tile.WhiteRook);
										if (!nextState.IsWhiteInCheck())
										{
											result.Add(nextState);
										}
									}
									else
									{
										// White Pawn Taking Right
										nextState = CloneForNextMove();
										nextState.SetTile(x, y, Tile.Empty);
										nextState.SetTile(x + 1, y + 1, Tile.WhitePawn);
										if (!nextState.IsWhiteInCheck())
										{
											result.Add(nextState);
										}
									}
								}
								if (y == 4 && (blackEnPassantPawn == x - 1 || blackEnPassantPawn == x + 1))
								{
									// White Pawn Taking En Passant
									nextState = CloneForNextMove();
									nextState.SetTile(x, y, Tile.Empty);
									nextState.SetTile(blackEnPassantPawn, 4, Tile.Empty);
									nextState.SetTile(blackEnPassantPawn, 5, Tile.WhitePawn);
									if (!nextState.IsWhiteInCheck())
									{
										result.Add(nextState);
									}
								}
								break;

							case Tile.WhiteRook:
								if (y == 0 && x == 0)
								{
									castlesAvailableModifier = CastleMove.WhiteKingSide | CastleMove.BlackKingSide | CastleMove.BlackQueenSide;
								}
								else if (y == 0 && x == 7)
								{
									castlesAvailableModifier = CastleMove.WhiteQueenSide | CastleMove.BlackKingSide | CastleMove.BlackQueenSide;
								}

								for (tx = x - 1; tx >= 0; tx--)
								{
									if (GetTile(tx, y) == Tile.Empty)
									{
										// White Rook Moving
										nextState = CloneForNextMove();
										nextState.SetTile(x, y, Tile.Empty);
										nextState.SetTile(tx, y, tile);
										if (!nextState.IsWhiteInCheck())
										{
											nextState.castlesAvailable &= castlesAvailableModifier;
											result.Add(nextState);
										}
									}
									else
									{
										// White Rook Taking
										if ((GetTile(tx, y) & Tile.White) == 0)
										{
											nextState = CloneForNextMove();
											nextState.SetTile(x, y, Tile.Empty);
											nextState.SetTile(tx, y, tile);
											nextState.castlesAvailable &= castlesAvailableModifier;
											if (!nextState.IsWhiteInCheck())
											{
												result.Add(nextState);
											}
										}
										break;
									}
								}
								for (tx = x + 1; tx < 8; tx++)
								{
									if (GetTile(tx, y) == Tile.Empty)
									{
										// White Rook Moving
										nextState = CloneForNextMove();
										nextState.SetTile(x, y, Tile.Empty);
										nextState.SetTile(tx, y, tile);
										if (!nextState.IsWhiteInCheck())
										{
											nextState.castlesAvailable &= castlesAvailableModifier;
											result.Add(nextState);
										}
									}
									else
									{
										if ((GetTile(tx, y) & Tile.White) == 0)
										{
											// White Rook Taking
											nextState = CloneForNextMove();
											nextState.SetTile(x, y, Tile.Empty);
											nextState.SetTile(tx, y, tile);
											if (!nextState.IsWhiteInCheck())
											{
												nextState.castlesAvailable &= castlesAvailableModifier;
												result.Add(nextState);
											}
										}
										break;
									}
								}
								for (ty = y - 1; ty >= 0; ty--)
								{
									if (GetTile(x, ty) == Tile.Empty)
									{
										// White Rook Moving
										nextState = CloneForNextMove();
										nextState.SetTile(x, y, Tile.Empty);
										nextState.SetTile(x, ty, tile);
										if (!nextState.IsWhiteInCheck())
										{
											nextState.castlesAvailable &= castlesAvailableModifier;
											result.Add(nextState);
										}
									}
									else
									{
										if ((GetTile(x, ty) & Tile.White) == 0)
										{
											// White Rook Taking
											nextState = CloneForNextMove();
											nextState.SetTile(x, y, Tile.Empty);
											nextState.SetTile(x, ty, tile);
											if (!nextState.IsWhiteInCheck())
											{
												nextState.castlesAvailable &= castlesAvailableModifier;
												result.Add(nextState);
											}
										}
										break;
									}
								}
								for (ty = y + 1; ty < 8; ty++)
								{
									if (GetTile(x, ty) == Tile.Empty)
									{
										// White Rook Moving
										nextState = CloneForNextMove();
										nextState.SetTile(x, y, Tile.Empty);
										nextState.SetTile(x, ty, tile);
										if (!nextState.IsWhiteInCheck())
										{
											nextState.castlesAvailable &= castlesAvailableModifier;
											result.Add(nextState);
										}
									}
									else
									{
										if ((GetTile(x, ty) & Tile.White) == 0)
										{
											// White Rook Taking
											nextState = CloneForNextMove();
											nextState.SetTile(x, y, Tile.Empty);
											nextState.SetTile(x, ty, tile);
											if (!nextState.IsWhiteInCheck())
											{
												nextState.castlesAvailable &= castlesAvailableModifier;
												result.Add(nextState);
											}
										}
										break;
									}
								}
								break;

							case Tile.WhiteKnight:
								if (x >= 2)
								{
									if (y >= 1 && (GetTile(x - 2, y - 1) & Tile.White) == 0)
									{
										// White Knight Moving/Taking
										nextState = CloneForNextMove();
										nextState.SetTile(x, y, Tile.Empty);
										nextState.SetTile(x - 2, y - 1, tile);
										if (!nextState.IsWhiteInCheck())
										{
											result.Add(nextState);
										}
									}
									if (y <= 6 && (GetTile(x - 2, y + 1) & Tile.White) == 0)
									{
										// White Knight Moving/Taking
										nextState = CloneForNextMove();
										nextState.SetTile(x, y, Tile.Empty);
										nextState.SetTile(x - 2, y + 1, tile);
										if (!nextState.IsWhiteInCheck())
										{
											result.Add(nextState);
										}
									}
								}
								if (x >= 1)
								{
									if (y >= 2 && (GetTile(x - 1, y - 2) & Tile.White) == 0)
									{
										// White Knight Moving/Taking
										nextState = CloneForNextMove();
										nextState.SetTile(x, y, Tile.Empty);
										nextState.SetTile(x - 1, y - 2, tile);
										if (!nextState.IsWhiteInCheck())
										{
											result.Add(nextState);
										}
									}
									if (y <= 5 && (GetTile(x - 1, y + 2) & Tile.White) == 0)
									{
										// White Knight Moving/Taking
										nextState = CloneForNextMove();
										nextState.SetTile(x, y, Tile.Empty);
										nextState.SetTile(x - 1, y + 2, tile);
										if (!nextState.IsWhiteInCheck())
										{
											result.Add(nextState);
										}
									}
								}
								if (x <= 5)
								{
									if (y >= 1 && (GetTile(x + 2, y - 1) & Tile.White) == 0)
									{
										// White Knight Moving/Taking
										nextState = CloneForNextMove();
										nextState.SetTile(x, y, Tile.Empty);
										nextState.SetTile(x + 2, y - 1, tile);
										if (!nextState.IsWhiteInCheck())
										{
											result.Add(nextState);
										}
									}
									if (y <= 6 && (GetTile(x + 2, y + 1) & Tile.White) == 0)
									{
										// White Knight Moving/Taking
										nextState = CloneForNextMove();
										nextState.SetTile(x, y, Tile.Empty);
										nextState.SetTile(x + 2, y + 1, tile);
										if (!nextState.IsWhiteInCheck())
										{
											result.Add(nextState);
										}
									}
								}
								if (x <= 6)
								{
									if (y >= 2 && (GetTile(x + 1, y - 2) & Tile.White) == 0)
									{
										// White Knight Moving/Taking
										nextState = CloneForNextMove();
										nextState.SetTile(x, y, Tile.Empty);
										nextState.SetTile(x + 1, y - 2, tile);
										if (!nextState.IsWhiteInCheck())
										{
											result.Add(nextState);
										}
									}
									if (y <= 5 && (GetTile(x + 1, y + 2) & Tile.White) == 0)
									{
										// White Knight Moving/Taking
										nextState = CloneForNextMove();
										nextState.SetTile(x, y, Tile.Empty);
										nextState.SetTile(x + 1, y + 2, tile);
										if (!nextState.IsWhiteInCheck())
										{
											result.Add(nextState);
										}
									}
								}
								break;

							case Tile.WhiteBishop:
								tx = x - 1;
								ty = y - 1;
								while (tx >= 0 && ty >= 0)
								{
									if (GetTile(tx, ty) == Tile.Empty)
									{
										// White Bishop Moving
										nextState = CloneForNextMove();
										nextState.SetTile(x, y, Tile.Empty);
										nextState.SetTile(tx, ty, tile);
										if (!nextState.IsWhiteInCheck())
										{
											result.Add(nextState);
										}
									}
									else
									{
										if ((GetTile(tx, ty) & Tile.White) == 0)
										{
											// White Bishop Taking
											nextState = CloneForNextMove();
											nextState.SetTile(x, y, Tile.Empty);
											nextState.SetTile(tx, ty, tile);
											if (!nextState.IsWhiteInCheck())
											{
												result.Add(nextState);
											}
										}
										break;
									}
									tx--;
									ty--;
								}
								tx = x - 1;
								ty = y + 1;
								while (tx >= 0 && ty < 8)
								{
									if (GetTile(tx, ty) == Tile.Empty)
									{
										// White Bishop Moving
										nextState = CloneForNextMove();
										nextState.SetTile(x, y, Tile.Empty);
										nextState.SetTile(tx, ty, tile);
										if (!nextState.IsWhiteInCheck())
										{
											result.Add(nextState);
										}
									}
									else
									{
										if ((GetTile(tx, ty) & Tile.White) == 0)
										{
											// White Bishop Taking
											nextState = CloneForNextMove();
											nextState.SetTile(x, y, Tile.Empty);
											nextState.SetTile(tx, ty, tile);
											if (!nextState.IsWhiteInCheck())
											{
												result.Add(nextState);
											}
										}
										break;
									}
									tx--;
									ty++;
								}
								tx = x + 1;
								ty = y - 1;
								while (tx < 8 && ty >= 0)
								{
									if (GetTile(tx, ty) == Tile.Empty)
									{
										// White Bishop Moving
										nextState = CloneForNextMove();
										nextState.SetTile(x, y, Tile.Empty);
										nextState.SetTile(tx, ty, tile);
										if (!nextState.IsWhiteInCheck())
										{
											result.Add(nextState);
										}
									}
									else
									{
										if ((GetTile(tx, ty) & Tile.White) == 0)
										{
											// White Bishop Taking
											nextState = CloneForNextMove();
											nextState.SetTile(x, y, Tile.Empty);
											nextState.SetTile(tx, ty, tile);
											if (!nextState.IsWhiteInCheck())
											{
												result.Add(nextState);
											}
										}
										break;
									}
									tx++;
									ty--;
								}
								tx = x + 1;
								ty = y + 1;
								while (tx < 8 && ty < 8)
								{
									if (GetTile(tx, ty) == Tile.Empty)
									{
										// White Bishop Moving
										nextState = CloneForNextMove();
										nextState.SetTile(x, y, Tile.Empty);
										nextState.SetTile(tx, ty, tile);
										if (!nextState.IsWhiteInCheck())
										{
											result.Add(nextState);
										}
									}
									else
									{
										if ((GetTile(tx, ty) & Tile.White) == 0)
										{
											// White Bishop Taking
											nextState = CloneForNextMove();
											nextState.SetTile(x, y, Tile.Empty);
											nextState.SetTile(tx, ty, tile);
											if (!nextState.IsWhiteInCheck())
											{
												result.Add(nextState);
											}
										}
										break;
									}
									tx++;
									ty++;
								}
								break;

							case Tile.WhiteQueen:
								for (tx = x - 1; tx >= 0; tx--)
								{
									if (GetTile(tx, y) == Tile.Empty)
									{
										// White Queen Moving
										nextState = CloneForNextMove();
										nextState.SetTile(x, y, Tile.Empty);
										nextState.SetTile(tx, y, tile);
										if (!nextState.IsWhiteInCheck())
										{
											result.Add(nextState);
										}
									}
									else
									{
										if ((GetTile(tx, y) & Tile.White) == 0)
										{
											// White Queen Taking
											nextState = CloneForNextMove();
											nextState.SetTile(x, y, Tile.Empty);
											nextState.SetTile(tx, y, tile);
											if (!nextState.IsWhiteInCheck())
											{
												result.Add(nextState);
											}
										}
										break;
									}
								}
								for (tx = x + 1; tx < 8; tx++)
								{
									if (GetTile(tx, y) == Tile.Empty)
									{
										// White Queen Moving
										nextState = CloneForNextMove();
										nextState.SetTile(x, y, Tile.Empty);
										nextState.SetTile(tx, y, tile);
										if (!nextState.IsWhiteInCheck())
										{
											result.Add(nextState);
										}
									}
									else
									{
										if ((GetTile(tx, y) & Tile.White) == 0)
										{
											// White Queen Taking
											nextState = CloneForNextMove();
											nextState.SetTile(x, y, Tile.Empty);
											nextState.SetTile(tx, y, tile);
											if (!nextState.IsWhiteInCheck())
											{
												result.Add(nextState);
											}
										}
										break;
									}
								}
								for (ty = y - 1; ty >= 0; ty--)
								{
									if (GetTile(x, ty) == Tile.Empty)
									{
										// White Queen Moving
										nextState = CloneForNextMove();
										nextState.SetTile(x, y, Tile.Empty);
										nextState.SetTile(x, ty, tile);
										if (!nextState.IsWhiteInCheck())
										{
											result.Add(nextState);
										}
									}
									else
									{
										if ((GetTile(x, ty) & Tile.White) == 0)
										{
											// White Queen Taking
											nextState = CloneForNextMove();
											nextState.SetTile(x, y, Tile.Empty);
											nextState.SetTile(x, ty, tile);
											if (!nextState.IsWhiteInCheck())
											{
												result.Add(nextState);
											}
										}
										break;
									}
								}
								for (ty = y + 1; ty < 8; ty++)
								{
									if (GetTile(x, ty) == Tile.Empty)
									{
										// White Queen Moving
										nextState = CloneForNextMove();
										nextState.SetTile(x, y, Tile.Empty);
										nextState.SetTile(x, ty, tile);
										if (!nextState.IsWhiteInCheck())
										{
											result.Add(nextState);
										}
									}
									else
									{
										if ((GetTile(x, ty) & Tile.White) == 0)
										{
											// White Queen Taking
											nextState = CloneForNextMove();
											nextState.SetTile(x, y, Tile.Empty);
											nextState.SetTile(x, ty, tile);
											if (!nextState.IsWhiteInCheck())
											{
												result.Add(nextState);
											}
										}
										break;
									}
								}
								tx = x - 1;
								ty = y - 1;
								while (tx >= 0 && ty >= 0)
								{
									if (GetTile(tx, ty) == Tile.Empty)
									{
										// White Queen Moving
										nextState = CloneForNextMove();
										nextState.SetTile(x, y, Tile.Empty);
										nextState.SetTile(tx, ty, tile);
										if (!nextState.IsWhiteInCheck())
										{
											result.Add(nextState);
										}
									}
									else
									{
										if ((GetTile(tx, ty) & Tile.White) == 0)
										{
											// White Queen Taking
											nextState = CloneForNextMove();
											nextState.SetTile(x, y, Tile.Empty);
											nextState.SetTile(tx, ty, tile);
											if (!nextState.IsWhiteInCheck())
											{
												result.Add(nextState);
											}
										}
										break;
									}
									tx--;
									ty--;
								}
								tx = x - 1;
								ty = y + 1;
								while (tx >= 0 && ty < 8)
								{
									if (GetTile(tx, ty) == Tile.Empty)
									{
										// White Queen Moving
										nextState = CloneForNextMove();
										nextState.SetTile(x, y, Tile.Empty);
										nextState.SetTile(tx, ty, tile);
										if (!nextState.IsWhiteInCheck())
										{
											result.Add(nextState);
										}
									}
									else
									{
										if ((GetTile(tx, ty) & Tile.White) == 0)
										{
											// White Queen Taking
											nextState = CloneForNextMove();
											nextState.SetTile(x, y, Tile.Empty);
											nextState.SetTile(tx, ty, tile);
											if (!nextState.IsWhiteInCheck())
											{
												result.Add(nextState);
											}
										}
										break;
									}
									tx--;
									ty++;
								}
								tx = x + 1;
								ty = y - 1;
								while (tx < 8 && ty >= 0)
								{
									if (GetTile(tx, ty) == Tile.Empty)
									{
										// White Queen Moving
										nextState = CloneForNextMove();
										nextState.SetTile(x, y, Tile.Empty);
										nextState.SetTile(tx, ty, tile);
										if (!nextState.IsWhiteInCheck())
										{
											result.Add(nextState);
										}
									}
									else
									{
										if ((GetTile(tx, ty) & Tile.White) == 0)
										{
											// White Queen Taking
											nextState = CloneForNextMove();
											nextState.SetTile(x, y, Tile.Empty);
											nextState.SetTile(tx, ty, tile);
											if (!nextState.IsWhiteInCheck())
											{
												result.Add(nextState);
											}
										}
										break;
									}
									tx++;
									ty--;
								}
								tx = x + 1;
								ty = y + 1;
								while (tx < 8 && ty < 8)
								{
									if (GetTile(tx, ty) == Tile.Empty)
									{
										// White Queen Moving
										nextState = CloneForNextMove();
										nextState.SetTile(x, y, Tile.Empty);
										nextState.SetTile(tx, ty, tile);
										if (!nextState.IsWhiteInCheck())
										{
											result.Add(nextState);
										}
									}
									else
									{
										if ((GetTile(tx, ty) & Tile.White) == 0)
										{
											// White Queen Taking
											nextState = CloneForNextMove();
											nextState.SetTile(x, y, Tile.Empty);
											nextState.SetTile(tx, ty, tile);
											if (!nextState.IsWhiteInCheck())
											{
												result.Add(nextState);
											}
										}
										break;
									}
									tx++;
									ty++;
								}
								break;

							case Tile.WhiteKing:
								castlesAvailableModifier = CastleMove.BlackKingSide | CastleMove.BlackQueenSide;
								if (y >= 1 && (GetTile(x, y - 1) & Tile.White) == 0)
								{
									// White King Moving/Taking
									nextState = CloneForNextMove();
									nextState.SetTile(x, y, Tile.Empty);
									nextState.SetTile(x, y - 1, tile);
									nextState.whiteKingX = x; nextState.whiteKingY = y - 1;
									if (!nextState.IsWhiteInCheck())
									{
										nextState.castlesAvailable &= castlesAvailableModifier;
										result.Add(nextState);
									}
								}
								if (y <= 6 && (GetTile(x, y + 1) & Tile.White) == 0)
								{
									// White King Moving/Taking
									nextState = CloneForNextMove();
									nextState.SetTile(x, y, Tile.Empty);
									nextState.SetTile(x, y + 1, tile);
									nextState.whiteKingX = x; nextState.whiteKingY = y + 1;
									if (!nextState.IsWhiteInCheck())
									{
										nextState.castlesAvailable &= castlesAvailableModifier;
										result.Add(nextState);
									}
								}
								if (x >= 1)
								{
									if ((GetTile(x - 1, y) & Tile.White) == 0)
									{
										// White King Moving/Taking
										nextState = CloneForNextMove();
										nextState.SetTile(x, y, Tile.Empty);
										nextState.SetTile(x - 1, y, tile);
										nextState.whiteKingX = x - 1; nextState.whiteKingY = y;
										if (!nextState.IsWhiteInCheck())
										{
											nextState.castlesAvailable &= castlesAvailableModifier;
											result.Add(nextState);
										}
									}
									if (y >= 1 && (GetTile(x - 1, y - 1) & Tile.White) == 0)
									{
										// White King Moving/Taking
										nextState = CloneForNextMove();
										nextState.SetTile(x, y, Tile.Empty);
										nextState.SetTile(x - 1, y - 1, tile);
										nextState.whiteKingX = x - 1; nextState.whiteKingY = y - 1;
										if (!nextState.IsWhiteInCheck())
										{
											nextState.castlesAvailable &= castlesAvailableModifier;
											result.Add(nextState);
										}
									}
									if (y <= 6 && (GetTile(x - 1, y + 1) & Tile.White) == 0)
									{
										// White King Moving/Taking
										nextState = CloneForNextMove();
										nextState.SetTile(x, y, Tile.Empty);
										nextState.SetTile(x - 1, y + 1, tile);
										nextState.whiteKingX = x - 1; nextState.whiteKingY = y + 1;
										if (!nextState.IsWhiteInCheck())
										{
											nextState.castlesAvailable &= castlesAvailableModifier;
											result.Add(nextState);
										}
									}
								}
								if (x <= 6)
								{
									if ((GetTile(x + 1, y) & Tile.White) == 0)
									{
										// White King Moving/Taking
										nextState = CloneForNextMove();
										nextState.SetTile(x, y, Tile.Empty);
										nextState.SetTile(x + 1, y, tile);
										nextState.whiteKingX = x + 1; nextState.whiteKingY = y;
										if (!nextState.IsWhiteInCheck())
										{
											nextState.castlesAvailable &= castlesAvailableModifier;
											result.Add(nextState);
										}
									}
									if (y >= 1 && (GetTile(x + 1, y - 1) & Tile.White) == 0)
									{
										// White King Moving/Taking
										nextState = CloneForNextMove();
										nextState.SetTile(x, y, Tile.Empty);
										nextState.SetTile(x + 1, y - 1, tile);
										nextState.whiteKingX = x + 1; nextState.whiteKingY = y - 1;
										if (!nextState.IsWhiteInCheck())
										{
											nextState.castlesAvailable &= castlesAvailableModifier;
											result.Add(nextState);
										}
									}
									if (y <= 6 && (GetTile(x + 1, y + 1) & Tile.White) == 0)
									{
										// White King Moving/Taking
										nextState = CloneForNextMove();
										nextState.SetTile(x, y, Tile.Empty);
										nextState.SetTile(x + 1, y + 1, tile);
										nextState.whiteKingX = x + 1; nextState.whiteKingY = y + 1;
										if (!nextState.IsWhiteInCheck())
										{
											nextState.castlesAvailable &= castlesAvailableModifier;
											result.Add(nextState);
										}
									}
								}
								break;
						}
					}
				}
				if (!IsWhiteInCheck())
				{
					if ((castlesAvailable & CastleMove.WhiteKingSide) != 0 && GetTile(5, 0) == Tile.Empty && GetTile(6, 0) == Tile.Empty)
					{
						nextState = CloneForNextMove();
						nextState.SetTile(4, 0, Tile.Empty);
						nextState.SetTile(7, 0, Tile.Empty);
						nextState.SetTile(5, 0, Tile.WhiteKing);
						nextState.whiteKingX = 5; nextState.whiteKingY = 0;
						if (!nextState.IsWhiteInCheck())
						{
							nextState.SetTile(5, 0, Tile.WhiteRook);
							nextState.SetTile(6, 0, Tile.WhiteKing);
							nextState.whiteKingX = 6;
							if (!nextState.IsWhiteInCheck())
							{
								nextState.castlesAvailable &= ~CastleMove.WhiteKingSide;
								result.Add(nextState);
							}
						}
					}
					if ((castlesAvailable & CastleMove.WhiteQueenSide) != 0 && GetTile(1, 0) == Tile.Empty && GetTile(2, 0) == Tile.Empty && GetTile(3, 0) == Tile.Empty)
					{
						nextState = CloneForNextMove();
						nextState.SetTile(0, 0, Tile.Empty);
						nextState.SetTile(4, 0, Tile.Empty);
						nextState.SetTile(3, 0, Tile.WhiteKing);
						nextState.whiteKingX = 3; nextState.whiteKingY = 0;
						if (!nextState.IsWhiteInCheck())
						{
							nextState.SetTile(2, 0, Tile.WhiteKing);
							nextState.SetTile(3, 0, Tile.WhiteRook);
							nextState.whiteKingX = 2;
							if (!nextState.IsWhiteInCheck())
							{
								nextState.castlesAvailable &= ~CastleMove.WhiteQueenSide;
								result.Add(nextState);
							}
						}
					}
				}
			}
			else
			{
				for (x = 0; x < 8; x++)
				{
					for (y = 0; y < 8; y++)
					{
						Tile tile = GetTile(x, y);
						castlesAvailableModifier = CastleMove.WhiteKingSide | CastleMove.WhiteQueenSide | CastleMove.BlackKingSide | CastleMove.BlackQueenSide;
						switch (tile)
						{
							case Tile.BlackPawn:
								if (GetTile(x, y - 1) == Tile.Empty)
								{
									if (y == 1)
									{
										// Black Pawn Promotion
										nextState = CloneForNextMove();
										nextState.SetTile(x, 1, Tile.Empty);
										nextState.SetTile(x, 0, Tile.BlackQueen);
										if (!nextState.IsBlackInCheck())
										{
											result.Add(nextState);
										}

										nextState = CloneForNextMove();
										nextState.SetTile(x, 1, Tile.Empty);
										nextState.SetTile(x, 0, Tile.BlackKnight);
										if (!nextState.IsBlackInCheck())
										{
											result.Add(nextState);
										}

										nextState = CloneForNextMove();
										nextState.SetTile(x, 1, Tile.Empty);
										nextState.SetTile(x, 0, Tile.BlackBishop);
										if (!nextState.IsBlackInCheck())
										{
											result.Add(nextState);
										}

										nextState = CloneForNextMove();
										nextState.SetTile(x, 1, Tile.Empty);
										nextState.SetTile(x, 0, Tile.BlackRook);
										if (!nextState.IsBlackInCheck())
										{
											result.Add(nextState);
										}
									}
									else
									{
										// Black Pawn Forward One
										nextState = CloneForNextMove();
										nextState.SetTile(x, y, Tile.Empty);
										nextState.SetTile(x, y - 1, Tile.BlackPawn);
										if (!nextState.IsBlackInCheck())
										{
											result.Add(nextState);
										}

										if (y == 6 && GetTile(x, 4) == Tile.Empty)
										{
											// Black Pawn Forward Two
											nextState = CloneForNextMove();
											nextState.SetTile(x, 6, Tile.Empty);
											nextState.SetTile(x, 4, Tile.BlackPawn);
											if (!nextState.IsBlackInCheck())
											{
												nextState.blackEnPassantPawn = x;
												result.Add(nextState);
											}
										}
									}
								}
								if (x != 7 && (GetTile(x + 1, y - 1) & Tile.White) > 0)
								{
									if (y == 1)
									{
										// Black Pawn Taking Left Promotion
										nextState = CloneForNextMove();
										nextState.SetTile(x, 1, Tile.Empty);
										nextState.SetTile(x + 1, 0, Tile.BlackQueen);
										if (!nextState.IsBlackInCheck())
										{
											result.Add(nextState);
										}

										nextState = CloneForNextMove();
										nextState.SetTile(x, 1, Tile.Empty);
										nextState.SetTile(x + 1, 0, Tile.BlackKnight);
										if (!nextState.IsBlackInCheck())
										{
											result.Add(nextState);
										}

										nextState = CloneForNextMove();
										nextState.SetTile(x, 1, Tile.Empty);
										nextState.SetTile(x + 1, 0, Tile.BlackBishop);
										if (!nextState.IsBlackInCheck())
										{
											result.Add(nextState);
										}

										nextState = CloneForNextMove();
										nextState.SetTile(x, 1, Tile.Empty);
										nextState.SetTile(x + 1, 0, Tile.BlackRook);
										if (!nextState.IsBlackInCheck())
										{
											result.Add(nextState);
										}
									}
									else
									{
										// Black Pawn Taking Left
										nextState = CloneForNextMove();
										nextState.SetTile(x, y, Tile.Empty);
										nextState.SetTile(x + 1, y - 1, Tile.BlackPawn);
										if (!nextState.IsBlackInCheck())
										{
											result.Add(nextState);
										}
									}
								}
								if (x != 0 && (GetTile(x - 1, y - 1) & Tile.White) > 0)
								{
									if (y == 1)
									{
										// Black Pawn Taking Right Promotion
										nextState = CloneForNextMove();
										nextState.SetTile(x, 1, Tile.Empty);
										nextState.SetTile(x - 1, 0, Tile.BlackQueen);
										if (!nextState.IsBlackInCheck())
										{
											result.Add(nextState);
										}

										nextState = CloneForNextMove();
										nextState.SetTile(x, 1, Tile.Empty);
										nextState.SetTile(x - 1, 0, Tile.BlackKnight);
										if (!nextState.IsBlackInCheck())
										{
											result.Add(nextState);
										}

										nextState = CloneForNextMove();
										nextState.SetTile(x, 1, Tile.Empty);
										nextState.SetTile(x - 1, 0, Tile.BlackBishop);
										if (!nextState.IsBlackInCheck())
										{
											result.Add(nextState);
										}

										nextState = CloneForNextMove();
										nextState.SetTile(x, 1, Tile.Empty);
										nextState.SetTile(x - 1, 0, Tile.BlackRook);
										if (!nextState.IsBlackInCheck())
										{
											result.Add(nextState);
										}
									}
									else
									{
										// Black Pawn Taking Right
										nextState = CloneForNextMove();
										nextState.SetTile(x, y, Tile.Empty);
										nextState.SetTile(x - 1, y - 1, Tile.BlackPawn);
										if (!nextState.IsBlackInCheck())
										{
											result.Add(nextState);
										}
									}
								}
								if (y == 3 && (whiteEnPassantPawn == x - 1 || whiteEnPassantPawn == x + 1))
								{
									// Black Pawn Taking En Passant
									nextState = CloneForNextMove();
									nextState.SetTile(x, y, Tile.Empty);
									nextState.SetTile(whiteEnPassantPawn, 3, Tile.Empty);
									nextState.SetTile(whiteEnPassantPawn, 2, Tile.BlackPawn);
									if (!nextState.IsBlackInCheck())
									{
										result.Add(nextState);
									}
								}
								break;

							case Tile.BlackRook:
								if (y == 7 && x == 0)
								{
									castlesAvailableModifier = CastleMove.WhiteKingSide | CastleMove.WhiteQueenSide | CastleMove.BlackKingSide;
								}
								else if (y == 7 && x == 7)
								{
									castlesAvailableModifier = CastleMove.WhiteKingSide | CastleMove.WhiteQueenSide | CastleMove.BlackQueenSide;
								}

								for (tx = x - 1; tx >= 0; tx--)
								{
									if (GetTile(tx, y) == Tile.Empty)
									{
										// Black Rook Moving
										nextState = CloneForNextMove();
										nextState.SetTile(x, y, Tile.Empty);
										nextState.SetTile(tx, y, tile);
										if (!nextState.IsBlackInCheck())
										{
											nextState.castlesAvailable &= castlesAvailableModifier;
											result.Add(nextState);
										}
									}
									else
									{
										if ((GetTile(tx, y) & Tile.Black) == 0)
										{
											// Black Rook Taking
											nextState = CloneForNextMove();
											nextState.SetTile(x, y, Tile.Empty);
											nextState.SetTile(tx, y, tile);
											if (!nextState.IsBlackInCheck())
											{
												nextState.castlesAvailable &= castlesAvailableModifier;
												result.Add(nextState);
											}
										}
										break;
									}
								}
								for (tx = x + 1; tx < 8; tx++)
								{
									if (GetTile(tx, y) == Tile.Empty)
									{
										// Black Rook Moving
										nextState = CloneForNextMove();
										nextState.SetTile(x, y, Tile.Empty);
										nextState.SetTile(tx, y, tile);
										if (!nextState.IsBlackInCheck())
										{
											nextState.castlesAvailable &= castlesAvailableModifier;
											result.Add(nextState);
										}
									}
									else
									{
										if ((GetTile(tx, y) & Tile.Black) == 0)
										{
											// Black Rook Taking
											nextState = CloneForNextMove();
											nextState.SetTile(x, y, Tile.Empty);
											nextState.SetTile(tx, y, tile);
											if (!nextState.IsBlackInCheck())
											{
												nextState.castlesAvailable &= castlesAvailableModifier;
												result.Add(nextState);
											}
										}
										break;
									}
								}
								for (ty = y - 1; ty >= 0; ty--)
								{
									if (GetTile(x, ty) == Tile.Empty)
									{
										// Black Rook Moving
										nextState = CloneForNextMove();
										nextState.SetTile(x, y, Tile.Empty);
										nextState.SetTile(x, ty, tile);
										if (!nextState.IsBlackInCheck())
										{
											nextState.castlesAvailable &= castlesAvailableModifier;
											result.Add(nextState);
										}
									}
									else
									{
										if ((GetTile(x, ty) & Tile.Black) == 0)
										{
											// Black Rook Taking
											nextState = CloneForNextMove();
											nextState.SetTile(x, y, Tile.Empty);
											nextState.SetTile(x, ty, tile);
											if (!nextState.IsBlackInCheck())
											{
												nextState.castlesAvailable &= castlesAvailableModifier;
												result.Add(nextState);
											}
										}
										break;
									}
								}
								for (ty = y + 1; ty < 8; ty++)
								{
									if (GetTile(x, ty) == Tile.Empty)
									{
										// Black Rook Moving
										nextState = CloneForNextMove();
										nextState.SetTile(x, y, Tile.Empty);
										nextState.SetTile(x, ty, tile);
										if (!nextState.IsBlackInCheck())
										{
											nextState.castlesAvailable &= castlesAvailableModifier;
											result.Add(nextState);
										}
									}
									else
									{
										if ((GetTile(x, ty) & Tile.Black) == 0)
										{
											// Black Rook Taking
											nextState = CloneForNextMove();
											nextState.SetTile(x, y, Tile.Empty);
											nextState.SetTile(x, ty, tile);
											if (!nextState.IsBlackInCheck())
											{
												nextState.castlesAvailable &= castlesAvailableModifier;
												result.Add(nextState);
											}
										}
										break;
									}
								}
								break;

							case Tile.BlackKnight:
								if (x >= 2)
								{
									if (y >= 1 && (GetTile(x - 2, y - 1) & Tile.Black) == 0)
									{
										// Black Knight Moving/Taking
										nextState = CloneForNextMove();
										nextState.SetTile(x, y, Tile.Empty);
										nextState.SetTile(x - 2, y - 1, tile);
										if (!nextState.IsBlackInCheck())
										{
											result.Add(nextState);
										}
									}
									if (y <= 6 && (GetTile(x - 2, y + 1) & Tile.Black) == 0)
									{
										// Black Knight Moving/Taking
										nextState = CloneForNextMove();
										nextState.SetTile(x, y, Tile.Empty);
										nextState.SetTile(x - 2, y + 1, tile);
										if (!nextState.IsBlackInCheck())
										{
											result.Add(nextState);
										}
									}
								}
								if (x >= 1)
								{
									if (y >= 2 && (GetTile(x - 1, y - 2) & Tile.Black) == 0)
									{
										// Black Knight Moving/Taking
										nextState = CloneForNextMove();
										nextState.SetTile(x, y, Tile.Empty);
										nextState.SetTile(x - 1, y - 2, tile);
										if (!nextState.IsBlackInCheck())
										{
											result.Add(nextState);
										}
									}
									if (y <= 5 && (GetTile(x - 1, y + 2) & Tile.Black) == 0)
									{
										// Black Knight Moving/Taking
										nextState = CloneForNextMove();
										nextState.SetTile(x, y, Tile.Empty);
										nextState.SetTile(x - 1, y + 2, tile);
										if (!nextState.IsBlackInCheck())
										{
											result.Add(nextState);
										}
									}
								}
								if (x <= 5)
								{
									if (y >= 1 && (GetTile(x + 2, y - 1) & Tile.Black) == 0)
									{
										// Black Knight Moving/Taking
										nextState = CloneForNextMove();
										nextState.SetTile(x, y, Tile.Empty);
										nextState.SetTile(x + 2, y - 1, tile);
										if (!nextState.IsBlackInCheck())
										{
											result.Add(nextState);
										}
									}
									if (y <= 6 && (GetTile(x + 2, y + 1) & Tile.Black) == 0)
									{
										// Black Knight Moving/Taking
										nextState = CloneForNextMove();
										nextState.SetTile(x, y, Tile.Empty);
										nextState.SetTile(x + 2, y + 1, tile);
										if (!nextState.IsBlackInCheck())
										{
											result.Add(nextState);
										}
									}
								}
								if (x <= 6)
								{
									if (y >= 2 && (GetTile(x + 1, y - 2) & Tile.Black) == 0)
									{
										// Black Knight Moving/Taking
										nextState = CloneForNextMove();
										nextState.SetTile(x, y, Tile.Empty);
										nextState.SetTile(x + 1, y - 2, tile);
										if (!nextState.IsBlackInCheck())
										{
											result.Add(nextState);
										}
									}
									if (y <= 5 && (GetTile(x + 1, y + 2) & Tile.Black) == 0)
									{
										// Black Knight Moving/Taking
										nextState = CloneForNextMove();
										nextState.SetTile(x, y, Tile.Empty);
										nextState.SetTile(x + 1, y + 2, tile);
										if (!nextState.IsBlackInCheck())
										{
											result.Add(nextState);
										}
									}
								}
								break;

							case Tile.BlackBishop:
								tx = x - 1;
								ty = y - 1;
								while (tx >= 0 && ty >= 0)
								{
									if (GetTile(tx, ty) == Tile.Empty)
									{
										// Black Bishop Moving
										nextState = CloneForNextMove();
										nextState.SetTile(x, y, Tile.Empty);
										nextState.SetTile(tx, ty, tile);
										if (!nextState.IsBlackInCheck())
										{
											result.Add(nextState);
										}
									}
									else
									{
										if ((GetTile(tx, ty) & Tile.Black) == 0)
										{
											// Black Bishop Taking
											nextState = CloneForNextMove();
											nextState.SetTile(x, y, Tile.Empty);
											nextState.SetTile(tx, ty, tile);
											if (!nextState.IsBlackInCheck())
											{
												result.Add(nextState);
											}
										}
										break;
									}
									tx--;
									ty--;
								}
								tx = x - 1;
								ty = y + 1;
								while (tx >= 0 && ty < 8)
								{
									if (GetTile(tx, ty) == Tile.Empty)
									{
										// Black Bishop Moving
										nextState = CloneForNextMove();
										nextState.SetTile(x, y, Tile.Empty);
										nextState.SetTile(tx, ty, tile);
										if (!nextState.IsBlackInCheck())
										{
											result.Add(nextState);
										}
									}
									else
									{
										if ((GetTile(tx, ty) & Tile.Black) == 0)
										{
											// Black Bishop Taking
											nextState = CloneForNextMove();
											nextState.SetTile(x, y, Tile.Empty);
											nextState.SetTile(tx, ty, tile);
											if (!nextState.IsBlackInCheck())
											{
												result.Add(nextState);
											}
										}
										break;
									}
									tx--;
									ty++;
								}
								tx = x + 1;
								ty = y - 1;
								while (tx < 8 && ty >= 0)
								{
									if (GetTile(tx, ty) == Tile.Empty)
									{
										// Black Bishop Moving
										nextState = CloneForNextMove();
										nextState.SetTile(x, y, Tile.Empty);
										nextState.SetTile(tx, ty, tile);
										if (!nextState.IsBlackInCheck())
										{
											result.Add(nextState);
										}
									}
									else
									{
										if ((GetTile(tx, ty) & Tile.Black) == 0)
										{
											// Black Bishop Taking
											nextState = CloneForNextMove();
											nextState.SetTile(x, y, Tile.Empty);
											nextState.SetTile(tx, ty, tile);
											if (!nextState.IsBlackInCheck())
											{
												result.Add(nextState);
											}
										}
										break;
									}
									tx++;
									ty--;
								}
								tx = x + 1;
								ty = y + 1;
								while (tx < 8 && ty < 8)
								{
									if (GetTile(tx, ty) == Tile.Empty)
									{
										// Black Bishop Moving
										nextState = CloneForNextMove();
										nextState.SetTile(x, y, Tile.Empty);
										nextState.SetTile(tx, ty, tile);
										if (!nextState.IsBlackInCheck())
										{
											result.Add(nextState);
										}
									}
									else
									{
										if ((GetTile(tx, ty) & Tile.Black) == 0)
										{
											// Black Bishop Taking
											nextState = CloneForNextMove();
											nextState.SetTile(x, y, Tile.Empty);
											nextState.SetTile(tx, ty, tile);
											if (!nextState.IsBlackInCheck())
											{
												result.Add(nextState);
											}
										}
										break;
									}
									tx++;
									ty++;
								}
								break;

							case Tile.BlackQueen:
								for (tx = x - 1; tx >= 0; tx--)
								{
									if (GetTile(tx, y) == Tile.Empty)
									{
										// Black Queen Moving
										nextState = CloneForNextMove();
										nextState.SetTile(x, y, Tile.Empty);
										nextState.SetTile(tx, y, tile);
										if (!nextState.IsBlackInCheck())
										{
											result.Add(nextState);
										}
									}
									else
									{
										if ((GetTile(tx, y) & Tile.Black) == 0)
										{
											// Black Queen Taking
											nextState = CloneForNextMove();
											nextState.SetTile(x, y, Tile.Empty);
											nextState.SetTile(tx, y, tile);
											if (!nextState.IsBlackInCheck())
											{
												result.Add(nextState);
											}
										}
										break;
									}
								}
								for (tx = x + 1; tx < 8; tx++)
								{
									if (GetTile(tx, y) == Tile.Empty)
									{
										// Black Queen Moving
										nextState = CloneForNextMove();
										nextState.SetTile(x, y, Tile.Empty);
										nextState.SetTile(tx, y, tile);
										if (!nextState.IsBlackInCheck())
										{
											result.Add(nextState);
										}
									}
									else
									{
										if ((GetTile(tx, y) & Tile.Black) == 0)
										{
											// Black Queen Taking
											nextState = CloneForNextMove();
											nextState.SetTile(x, y, Tile.Empty);
											nextState.SetTile(tx, y, tile);
											if (!nextState.IsBlackInCheck())
											{
												result.Add(nextState);
											}
										}
										break;
									}
								}
								for (ty = y - 1; ty >= 0; ty--)
								{
									if (GetTile(x, ty) == Tile.Empty)
									{
										// Black Queen Moving
										nextState = CloneForNextMove();
										nextState.SetTile(x, y, Tile.Empty);
										nextState.SetTile(x, ty, tile);
										if (!nextState.IsBlackInCheck())
										{
											result.Add(nextState);
										}
									}
									else
									{
										if ((GetTile(x, ty) & Tile.Black) == 0)
										{
											// Black Queen Taking
											nextState = CloneForNextMove();
											nextState.SetTile(x, y, Tile.Empty);
											nextState.SetTile(x, ty, tile);
											if (!nextState.IsBlackInCheck())
											{
												result.Add(nextState);
											}
										}
										break;
									}
								}
								for (ty = y + 1; ty < 8; ty++)
								{
									if (GetTile(x, ty) == Tile.Empty)
									{
										// Black Queen Moving
										nextState = CloneForNextMove();
										nextState.SetTile(x, y, Tile.Empty);
										nextState.SetTile(x, ty, tile);
										if (!nextState.IsBlackInCheck())
										{
											result.Add(nextState);
										}
									}
									else
									{
										if ((GetTile(x, ty) & Tile.Black) == 0)
										{
											// Black Queen Taking
											nextState = CloneForNextMove();
											nextState.SetTile(x, y, Tile.Empty);
											nextState.SetTile(x, ty, tile);
											if (!nextState.IsBlackInCheck())
											{
												result.Add(nextState);
											}
										}
										break;
									}
								}
								tx = x - 1;
								ty = y - 1;
								while (tx >= 0 && ty >= 0)
								{
									if (GetTile(tx, ty) == Tile.Empty)
									{
										// Black Queen Moving
										nextState = CloneForNextMove();
										nextState.SetTile(x, y, Tile.Empty);
										nextState.SetTile(tx, ty, tile);
										if (!nextState.IsBlackInCheck())
										{
											result.Add(nextState);
										}
									}
									else
									{
										if ((GetTile(tx, ty) & Tile.Black) == 0)
										{
											// Black Queen Taking
											nextState = CloneForNextMove();
											nextState.SetTile(x, y, Tile.Empty);
											nextState.SetTile(tx, ty, tile);
											if (!nextState.IsBlackInCheck())
											{
												result.Add(nextState);
											}
										}
										break;
									}
									tx--;
									ty--;
								}
								tx = x - 1;
								ty = y + 1;
								while (tx >= 0 && ty < 8)
								{
									if (GetTile(tx, ty) == Tile.Empty)
									{
										// Black Queen Moving
										nextState = CloneForNextMove();
										nextState.SetTile(x, y, Tile.Empty);
										nextState.SetTile(tx, ty, tile);
										if (!nextState.IsBlackInCheck())
										{
											result.Add(nextState);
										}
									}
									else
									{
										if ((GetTile(tx, ty) & Tile.Black) == 0)
										{
											// Black Queen Taking
											nextState = CloneForNextMove();
											nextState.SetTile(x, y, Tile.Empty);
											nextState.SetTile(tx, ty, tile);
											if (!nextState.IsBlackInCheck())
											{
												result.Add(nextState);
											}
										}
										break;
									}
									tx--;
									ty++;
								}
								tx = x + 1;
								ty = y - 1;
								while (tx < 8 && ty >= 0)
								{
									if (GetTile(tx, ty) == Tile.Empty)
									{
										// Black Queen Moving
										nextState = CloneForNextMove();
										nextState.SetTile(x, y, Tile.Empty);
										nextState.SetTile(tx, ty, tile);
										if (!nextState.IsBlackInCheck())
										{
											result.Add(nextState);
										}
									}
									else
									{
										if ((GetTile(tx, ty) & Tile.Black) == 0)
										{
											// Black Queen Taking
											nextState = CloneForNextMove();
											nextState.SetTile(x, y, Tile.Empty);
											nextState.SetTile(tx, ty, tile);
											if (!nextState.IsBlackInCheck())
											{
												result.Add(nextState);
											}
										}
										break;
									}
									tx++;
									ty--;
								}
								tx = x + 1;
								ty = y + 1;
								while (tx < 8 && ty < 8)
								{
									if (GetTile(tx, ty) == Tile.Empty)
									{
										// Black Queen Moving
										nextState = CloneForNextMove();
										nextState.SetTile(x, y, Tile.Empty);
										nextState.SetTile(tx, ty, tile);
										if (!nextState.IsBlackInCheck())
										{
											result.Add(nextState);
										}
									}
									else
									{
										if ((GetTile(tx, ty) & Tile.Black) == 0)
										{
											// Black Queen Taking
											nextState = CloneForNextMove();
											nextState.SetTile(x, y, Tile.Empty);
											nextState.SetTile(tx, ty, tile);
											if (!nextState.IsBlackInCheck())
											{
												result.Add(nextState);
											}
										}
										break;
									}
									tx++;
									ty++;
								}
								break;

							case Tile.BlackKing:
								castlesAvailableModifier = CastleMove.WhiteKingSide | CastleMove.WhiteQueenSide;
								if (y >= 1 && (GetTile(x, y - 1) & Tile.Black) == 0)
								{
									// Black King Moving/Taking
									nextState = CloneForNextMove();
									nextState.SetTile(x, y, Tile.Empty);
									nextState.SetTile(x, y - 1, tile);
									nextState.blackKingX = x; nextState.blackKingY = y - 1;
									if (!nextState.IsBlackInCheck())
									{
										nextState.castlesAvailable &= castlesAvailableModifier;
										result.Add(nextState);
									}
								}
								if (y <= 6 && (GetTile(x, y + 1) & Tile.Black) == 0)
								{
									// Black King Moving/Taking
									nextState = CloneForNextMove();
									nextState.SetTile(x, y, Tile.Empty);
									nextState.SetTile(x, y + 1, tile);
									nextState.blackKingX = x; nextState.blackKingY = y + 1;
									if (!nextState.IsBlackInCheck())
									{
										nextState.castlesAvailable &= castlesAvailableModifier;
										result.Add(nextState);
									}
								}
								if (x >= 1)
								{
									if ((GetTile(x - 1, y) & Tile.Black) == 0)
									{
										// Black King Moving/Taking
										nextState = CloneForNextMove();
										nextState.SetTile(x, y, Tile.Empty);
										nextState.SetTile(x - 1, y, tile);
										nextState.blackKingX = x - 1; nextState.blackKingY = y;
										if (!nextState.IsBlackInCheck())
										{
											nextState.castlesAvailable &= castlesAvailableModifier;
											result.Add(nextState);
										}
									}
									if (y >= 1 && (GetTile(x - 1, y - 1) & Tile.Black) == 0)
									{
										// Black King Moving/Taking
										nextState = CloneForNextMove();
										nextState.SetTile(x, y, Tile.Empty);
										nextState.SetTile(x - 1, y - 1, tile);
										nextState.blackKingX = x - 1; nextState.blackKingY = y - 1;
										if (!nextState.IsBlackInCheck())
										{
											nextState.castlesAvailable &= castlesAvailableModifier;
											result.Add(nextState);
										}
									}
									if (y <= 6 && (GetTile(x - 1, y + 1) & Tile.Black) == 0)
									{
										// Black King Moving/Taking
										nextState = CloneForNextMove();
										nextState.SetTile(x, y, Tile.Empty);
										nextState.SetTile(x - 1, y + 1, tile);
										nextState.blackKingX = x - 1; nextState.blackKingY = y + 1;
										if (!nextState.IsBlackInCheck())
										{
											nextState.castlesAvailable &= castlesAvailableModifier;
											result.Add(nextState);
										}
									}
								}
								if (x <= 6)
								{
									if ((GetTile(x + 1, y) & Tile.Black) == 0)
									{
										// Black King Moving/Taking
										nextState = CloneForNextMove();
										nextState.SetTile(x, y, Tile.Empty);
										nextState.SetTile(x + 1, y, tile);
										nextState.blackKingX = x + 1; nextState.blackKingY = y;
										if (!nextState.IsBlackInCheck())
										{
											nextState.castlesAvailable &= castlesAvailableModifier;
											result.Add(nextState);
										}
									}
									if (y >= 1 && (GetTile(x + 1, y - 1) & Tile.Black) == 0)
									{
										// Black King Moving/Taking
										nextState = CloneForNextMove();
										nextState.SetTile(x, y, Tile.Empty);
										nextState.SetTile(x + 1, y - 1, tile);
										nextState.blackKingX = x + 1; nextState.blackKingY = y - 1;
										if (!nextState.IsBlackInCheck())
										{
											nextState.castlesAvailable &= castlesAvailableModifier;
											result.Add(nextState);
										}
									}
									if (y <= 6 && (GetTile(x + 1, y + 1) & Tile.Black) == 0)
									{
										// Black King Moving/Taking
										nextState = CloneForNextMove();
										nextState.SetTile(x, y, Tile.Empty);
										nextState.SetTile(x + 1, y + 1, tile);
										nextState.blackKingX = x + 1; nextState.blackKingY = y + 1;
										if (!nextState.IsBlackInCheck())
										{
											nextState.castlesAvailable &= castlesAvailableModifier;
											result.Add(nextState);
										}
									}
								}
								break;
						}
					}
				}
				if (!IsBlackInCheck())
				{
					if ((castlesAvailable & CastleMove.BlackKingSide) != 0 && GetTile(5, 7) == Tile.Empty && GetTile(6, 7) == Tile.Empty)
					{
						nextState = CloneForNextMove();
						nextState.SetTile(4, 7, Tile.Empty);
						nextState.SetTile(7, 7, Tile.Empty);
						nextState.SetTile(5, 7, Tile.BlackKing);
						nextState.blackKingX = 5; nextState.blackKingY = 7;
						if (!nextState.IsBlackInCheck())
						{
							nextState.SetTile(5, 7, Tile.BlackRook);
							nextState.SetTile(6, 7, Tile.BlackKing);
							nextState.blackKingX = 6;
							if (!nextState.IsBlackInCheck())
							{
								nextState.castlesAvailable &= CastleMove.WhiteKingSide | CastleMove.WhiteQueenSide;
								result.Add(nextState);
							}
						}
					}
					if ((castlesAvailable & CastleMove.BlackQueenSide) != 0 && GetTile(1, 7) == Tile.Empty && GetTile(2, 7) == Tile.Empty && GetTile(3, 7) == Tile.Empty)
					{
						nextState = CloneForNextMove();
						nextState.SetTile(0, 7, Tile.Empty);
						nextState.SetTile(4, 7, Tile.Empty);
						nextState.SetTile(3, 7, Tile.BlackKing);
						nextState.blackKingX = 3; nextState.blackKingY = 7;
						if (!nextState.IsBlackInCheck())
						{
							nextState.SetTile(2, 7, Tile.BlackKing);
							nextState.SetTile(3, 7, Tile.BlackRook);
							nextState.blackKingX = 2;
							if (!nextState.IsBlackInCheck())
							{
								nextState.castlesAvailable &= CastleMove.BlackKingSide | CastleMove.BlackQueenSide;
								result.Add(nextState);
							}
						}
					}
				}
			}

			return result;
		}
	}
}
