// 
//  ShogiPosition.cs
//  
//  Author:
//       rootie <>
//  
//  Copyright (c) 2010 rootie
// 
//  This program is free software: you can redistribute it and/or modify
//  it under the terms of the GNU Lesser General Public License as published by
//  the Free Software Foundation, either version 3 of the License, or
//  (at your option) any later version.
// 
//  This program is distributed in the hope that it will be useful,
//  but WITHOUT ANY WARRANTY; without even the implied warranty of
//  MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
//  GNU Lesser General Public License for more details.
// 
//  You should have received a copy of the GNU Lesser General Public License
//  along with this program.  If not, see <http://www.gnu.org/licenses/>.
using System;
namespace Shogiban
{
	public class Position
	{
		private ValidMoves[,] ValidBoardMoves = new ValidMoves[Game.BOARD_SIZE, Game.BOARD_SIZE];
		private ValidMoves[,] ValidOnHandMoves = new ValidMoves[Game.PLAYER_COUNT, (int)PieceType.PIECE_TYPES_COUNT];
		private bool _Mate;
		private bool Dirty = true;
		
		public FieldInfo[,] Board = new FieldInfo[Game.BOARD_SIZE, Game.BOARD_SIZE];
		public int[,] OnHandPieces = new int[Game.PLAYER_COUNT, (int)PieceType.PIECE_TYPES_COUNT];
		//public PieceDirection CurPlayer = PieceDirection.UP;
		private PieceDirection _CurPlayer = PieceDirection.UP;
		//public PieceDirection CurPlayer
		public PieceDirection CurPlayer
		{
			get
			{
				return _CurPlayer;
			}
			set
			{
				_CurPlayer = value;
				System.Console.WriteLine(this.GetHashCode().ToString() + ": CurPlayer: " + _CurPlayer.ToString());
			}
		}	
		
		public bool Mate
		{
			get
			{
				if (Dirty)
				{
					UpdateValidMoves();
				}
				return _Mate;
			}
		}
		
		public Position Clone()
		{
			lock (this) //TODO just for testing
			{
				Position NewPos = new Position();
			
				NewPos.Board = (FieldInfo[,])Board.Clone();
				NewPos.OnHandPieces = (int[,])OnHandPieces.Clone();
				NewPos.CurPlayer = CurPlayer;
				
				return NewPos;
			}
		}
		
		public bool ApplyMove(Move move)
		{
			Console.WriteLine ("ShogiPosition: ApplyMove: " + move.ToString());
			if (!IsMoveValid(move))
			{
				return false;
			}
			
			//if a piece was captured add it to the own pieces on hand
			if (Board[move.To.x, move.To.y].Piece != PieceType.NONE)
			{
				AddOnHandPiece(CurPlayer, Board[move.To.x, move.To.y].Piece.GetUnpromotedPiece());
			}
			
			//check if the piece is from the hand or from the board
			if (move.OnHandPiece != PieceType.NONE)
			{
				Board[move.To.x, move.To.y].Piece = move.OnHandPiece;
				Board[move.To.x, move.To.y].Direction = CurPlayer;
				RemoveOnHandPiece(CurPlayer, move.OnHandPiece);
			}

			else
			{
				Board[move.To.x, move.To.y] = Board[move.From.x, move.From.y];
				if (move.promote)
				{
					Board[move.To.x, move.To.y].Piece = Board[move.To.x, move.To.y].Piece.GetPromotedPiece();
				}
				Board[move.From.x, move.From.y].Piece = PieceType.NONE;
			}
			
			CurPlayer = CurPlayer == PieceDirection.UP ? PieceDirection.DOWN : PieceDirection.UP;
			
			Dirty = true;
			
			return true;
		}
		
		public int CurrentPlayerNumber
		{
			get { return CurPlayer == PieceDirection.UP ? 0 : 1; }
		}
		
		public ValidMoves GetValidBoardMoves(BoardField From)
		{
			if (Dirty)
			{
				UpdateValidMoves();
			}
			//TODO clone
			return ValidBoardMoves[From.x, From.y];
		}

		public ValidMoves GetValidOnHandMoves(PieceType piece, bool BlackPlayer)
		{
			if (Dirty)
			{
				UpdateValidMoves();
			}
			//TODO clone
			return ValidOnHandMoves[BlackPlayer?0:1, (int)piece];
		}

		public bool IsMoveValid(Move move)
		{
			//validate move
			ValidMoves validMoves;
			
			if (move.OnHandPiece == PieceType.NONE)
			{
				//check if the piece is owned by the current player
				if (Board[move.From.x, move.From.y].Direction != CurPlayer)
					return false;
				
				//check (forced) promotion
				if (Board[move.From.x, move.From.y].Piece.CanPromote())
				{
					BoardField To = new BoardField(move.To.x, move.To.y);
					if (CheckForcedPromotion(To, Board[move.From.x, move.From.y].Piece, CurPlayer == PieceDirection.UP) && move.promote == false)
						return false;
					
					//these variables are turned as if seen from the
					//black player to ease checking
					int from_y_nor = move.From.y;
					int to_y_nor = move.To.y;
					if (CurPlayer == PieceDirection.DOWN)
					{
						from_y_nor = Game.BOARD_SIZE - from_y_nor - 1;
						to_y_nor = Game.BOARD_SIZE - to_y_nor - 1;
					}
				
					//no forced promotion
					//check if the player can promote
					if (move.promote == true && !(to_y_nor <= 2 || from_y_nor <= 2))
					{
						return false;
					}
				}
				else if (move.promote == true)
				{
					//piece can not promote
					return false;
				}

				validMoves = GetValidBoardMoves(move.From);
			}
			else
			{
				if (OnHandPieces[CurrentPlayerNumber, (int)move.OnHandPiece] <= 0)
					return false;
				if (move.promote == true)
					return false;

				validMoves = GetValidOnHandMoves(move.OnHandPiece, CurPlayer == PieceDirection.UP);
			}
			
			if (validMoves == null)
			{
				return false;
			}
			
			foreach (BoardField field in validMoves)
			{
				if (move.To.Equals(field))
				{
					return true;
				}
			}
			
			return false;
		}

		public bool CheckForcedPromotion(BoardField To, PieceType Piece, bool BlackPlayer)
		{
			//this variable is turned as if seen from the
			//black player to ease checking
			int to_y_nor = To.y;
			if (!BlackPlayer)
			{
				to_y_nor = Game.BOARD_SIZE - to_y_nor - 1;
			}

			//check for pawn or lance on the last line
			if (to_y_nor == 0 
			    && (Piece == PieceType.FUHYOU || Piece == PieceType.KYOUSHA))
			{
				return true;
			}
			//check for knight on the last 2 lines
			else if (to_y_nor <= 1 && Piece == PieceType.KEIMA)
			{
				return true;
			}
			
			return false;
		}

		//TODO remove dir parameter
		private static bool PieceCanMoveTo(FieldInfo[,] TmpBoard, BoardField To, PieceDirection dir, ValidMoves PieceMoves)
		{
			//check board bounds
			if (To.x < 0 || To.x >= Game.BOARD_SIZE || To.y < 0 || To.y >= Game.BOARD_SIZE)
				return false;
			
			PieceMoves.Add(To);

			//check if there is already a piece on the field
			if (TmpBoard[To.x, To.y].Piece != PieceType.NONE)
				return false;
			
			return true;
		}
		
		private static ValidMoves GetMovesForPiece(FieldInfo[,] TmpBoard, BoardField From)
		{
			ValidMoves PieceMoves = new ValidMoves();
			int Forward = TmpBoard[From.x, From.y].Direction == PieceDirection.UP ? -1 : 1;
			
			BoardField CurField;
			
			switch (TmpBoard[From.x, From.y].Piece)
			{
			case PieceType.OUSHOU:
				CurField = new BoardField(From.x, From.y + Forward);
				PieceCanMoveTo(TmpBoard, CurField, TmpBoard[From.x, From.y].Direction, PieceMoves);
				CurField = new BoardField(From.x + 1, From.y + Forward);
				PieceCanMoveTo(TmpBoard, CurField, TmpBoard[From.x, From.y].Direction, PieceMoves);
				CurField = new BoardField(From.x - 1, From.y + Forward);
				PieceCanMoveTo(TmpBoard, CurField, TmpBoard[From.x, From.y].Direction, PieceMoves);
				CurField = new BoardField(From.x + 1, From.y);
				PieceCanMoveTo(TmpBoard, CurField, TmpBoard[From.x, From.y].Direction, PieceMoves);
				CurField = new BoardField(From.x - 1, From.y);
				PieceCanMoveTo(TmpBoard, CurField, TmpBoard[From.x, From.y].Direction, PieceMoves);
				CurField = new BoardField(From.x, From.y - Forward);
				PieceCanMoveTo(TmpBoard, CurField, TmpBoard[From.x, From.y].Direction, PieceMoves);
				CurField = new BoardField(From.x + 1, From.y - Forward);
				PieceCanMoveTo(TmpBoard, CurField, TmpBoard[From.x, From.y].Direction, PieceMoves);
				CurField = new BoardField(From.x - 1, From.y - Forward);
				PieceCanMoveTo(TmpBoard, CurField, TmpBoard[From.x, From.y].Direction, PieceMoves);
				break;
			case PieceType.KINSHOU:
			case PieceType.NARIGIN:
			case PieceType.NARIKEI:
			case PieceType.NARIKYOU:
			case PieceType.TOKIN:
				CurField = new BoardField(From.x, From.y + Forward);
				PieceCanMoveTo(TmpBoard, CurField, TmpBoard[From.x, From.y].Direction, PieceMoves);
				CurField = new BoardField(From.x + 1, From.y + Forward);
				PieceCanMoveTo(TmpBoard, CurField, TmpBoard[From.x, From.y].Direction, PieceMoves);
				CurField = new BoardField(From.x - 1, From.y + Forward);
				PieceCanMoveTo(TmpBoard, CurField, TmpBoard[From.x, From.y].Direction, PieceMoves);
				CurField = new BoardField(From.x + 1, From.y);
				PieceCanMoveTo(TmpBoard, CurField, TmpBoard[From.x, From.y].Direction, PieceMoves);
				CurField = new BoardField(From.x - 1, From.y);
				PieceCanMoveTo(TmpBoard, CurField, TmpBoard[From.x, From.y].Direction, PieceMoves);
				CurField = new BoardField(From.x, From.y - Forward);
				PieceCanMoveTo(TmpBoard, CurField, TmpBoard[From.x, From.y].Direction, PieceMoves);
				break;
			case PieceType.GINSHOU:
				CurField = new BoardField(From.x, From.y + Forward);
				PieceCanMoveTo(TmpBoard, CurField, TmpBoard[From.x, From.y].Direction, PieceMoves);
				CurField = new BoardField(From.x + 1, From.y + Forward);
				PieceCanMoveTo(TmpBoard, CurField, TmpBoard[From.x, From.y].Direction, PieceMoves);
				CurField = new BoardField(From.x - 1, From.y + Forward);
				PieceCanMoveTo(TmpBoard, CurField, TmpBoard[From.x, From.y].Direction, PieceMoves);
				CurField = new BoardField(From.x + 1, From.y - Forward);
				PieceCanMoveTo(TmpBoard, CurField, TmpBoard[From.x, From.y].Direction, PieceMoves);
				CurField = new BoardField(From.x - 1, From.y - Forward);
				PieceCanMoveTo(TmpBoard, CurField, TmpBoard[From.x, From.y].Direction, PieceMoves);
				break;
			case PieceType.FUHYOU:
				CurField = new BoardField(From.x, From.y + Forward);
				PieceCanMoveTo(TmpBoard, CurField, TmpBoard[From.x, From.y].Direction, PieceMoves);
				break;
			case PieceType.KEIMA:
				CurField = new BoardField(From.x + 1, From.y + 2 * Forward);
				PieceCanMoveTo(TmpBoard, CurField, TmpBoard[From.x, From.y].Direction, PieceMoves);
				CurField = new BoardField(From.x - 1, From.y + 2 * Forward);
				PieceCanMoveTo(TmpBoard, CurField, TmpBoard[From.x, From.y].Direction, PieceMoves);
				break;
			case PieceType.KYOUSHA:
				CurField = new BoardField(From.x, From.y + Forward);
				while (PieceCanMoveTo(TmpBoard, CurField, TmpBoard[From.x, From.y].Direction, PieceMoves))
				{
					CurField = new BoardField(From.x, CurField.y + Forward);
				}
				break;
			case PieceType.HISHA:
				CurField = new BoardField(From.x, From.y + Forward);
				while (PieceCanMoveTo(TmpBoard, CurField, TmpBoard[From.x, From.y].Direction, PieceMoves))
				{
					CurField = new BoardField(From.x, CurField.y + Forward);
				}
				CurField = new BoardField(From.x, From.y - Forward);
				while (PieceCanMoveTo(TmpBoard, CurField, TmpBoard[From.x, From.y].Direction, PieceMoves))
				{
					CurField = new BoardField(From.x, CurField.y - Forward);
				}
				CurField = new BoardField(From.x + 1, From.y);
				while (PieceCanMoveTo(TmpBoard, CurField, TmpBoard[From.x, From.y].Direction, PieceMoves))
				{
					CurField = new BoardField(CurField.x + 1, CurField.y);
				}
				CurField = new BoardField(From.x - 1, From.y);
				while (PieceCanMoveTo(TmpBoard, CurField, TmpBoard[From.x, From.y].Direction, PieceMoves))
				{
					CurField = new BoardField(CurField.x - 1, CurField.y);
				}
				break;
			case PieceType.RYUUOU:
				//moves from hisha
				CurField = new BoardField(From.x, From.y + Forward);
				while (PieceCanMoveTo(TmpBoard, CurField, TmpBoard[From.x, From.y].Direction, PieceMoves))
				{
					CurField = new BoardField(CurField.x, CurField.y + Forward);
				}
				CurField = new BoardField(From.x, From.y - Forward);
				while (PieceCanMoveTo(TmpBoard, CurField, TmpBoard[From.x, From.y].Direction, PieceMoves))
				{
					CurField = new BoardField(CurField.x, CurField.y - Forward);
				}
				CurField = new BoardField(From.x + 1, From.y);
				while (PieceCanMoveTo(TmpBoard, CurField, TmpBoard[From.x, From.y].Direction, PieceMoves))
				{
					CurField = new BoardField(CurField.x + 1, CurField.y);
				}
				CurField = new BoardField(From.x - 1, From.y);
				while (PieceCanMoveTo(TmpBoard, CurField, TmpBoard[From.x, From.y].Direction, PieceMoves))
				{
					CurField = new BoardField(CurField.x - 1, CurField.y);
				}
				//ryuuou extra moves
				CurField = new BoardField(From.x + 1, From.y + Forward);
				PieceCanMoveTo(TmpBoard, CurField, TmpBoard[From.x, From.y].Direction, PieceMoves);
				CurField = new BoardField(From.x - 1, From.y + Forward);
				PieceCanMoveTo(TmpBoard, CurField, TmpBoard[From.x, From.y].Direction, PieceMoves);
				CurField = new BoardField(From.x + 1, From.y - Forward);
				PieceCanMoveTo(TmpBoard, CurField, TmpBoard[From.x, From.y].Direction, PieceMoves);
				CurField = new BoardField(From.x - 1, From.y - Forward);
				PieceCanMoveTo(TmpBoard, CurField, TmpBoard[From.x, From.y].Direction, PieceMoves);
				break;
			case PieceType.KAKUGYOU:
				CurField = new BoardField(From.x + 1, From.y + Forward);
				while (PieceCanMoveTo(TmpBoard, CurField, TmpBoard[From.x, From.y].Direction, PieceMoves))
				{
					CurField = new BoardField(CurField.x + 1, CurField.y + Forward);
				}
				CurField = new BoardField(From.x - 1, From.y + Forward);
				while (PieceCanMoveTo(TmpBoard, CurField, TmpBoard[From.x, From.y].Direction, PieceMoves))
				{
					CurField = new BoardField(CurField.x - 1, CurField.y + Forward);
				}
				CurField = new BoardField(From.x + 1, From.y - Forward);
				while (PieceCanMoveTo(TmpBoard, CurField, TmpBoard[From.x, From.y].Direction, PieceMoves))
				{
					CurField = new BoardField(CurField.x + 1, CurField.y - Forward);
				}
				CurField = new BoardField(From.x - 1, From.y - Forward);
				while (PieceCanMoveTo(TmpBoard, CurField, TmpBoard[From.x, From.y].Direction, PieceMoves))
				{
					CurField = new BoardField(CurField.x - 1, CurField.y - Forward);
				}
				break;
			case PieceType.RYUUMA:
				//moves from kakugyou
				CurField = new BoardField(From.x + 1, From.y + Forward);
				while (PieceCanMoveTo(TmpBoard, CurField, TmpBoard[From.x, From.y].Direction, PieceMoves))
				{
					CurField = new BoardField(CurField.x + 1, CurField.y + Forward);
				}
				CurField = new BoardField(From.x - 1, From.y + Forward);
				while (PieceCanMoveTo(TmpBoard, CurField, TmpBoard[From.x, From.y].Direction, PieceMoves))
				{
					CurField = new BoardField(CurField.x - 1, CurField.y + Forward);
				}
				CurField = new BoardField(From.x + 1, From.y - Forward);
				while (PieceCanMoveTo(TmpBoard, CurField, TmpBoard[From.x, From.y].Direction, PieceMoves))
				{
					CurField = new BoardField(CurField.x + 1, CurField.y - Forward);
				}
				CurField = new BoardField(From.x - 1, From.y - Forward);
				while (PieceCanMoveTo(TmpBoard, CurField, TmpBoard[From.x, From.y].Direction, PieceMoves))
				{
					CurField = new BoardField(CurField.x - 1, CurField.y - Forward);
				}
				//ryuuma extra moves
				CurField = new BoardField(From.x, From.y + Forward);
				PieceCanMoveTo(TmpBoard, CurField, TmpBoard[From.x, From.y].Direction, PieceMoves);
				CurField = new BoardField(From.x, From.y - Forward);
				PieceCanMoveTo(TmpBoard, CurField, TmpBoard[From.x, From.y].Direction, PieceMoves);
				CurField = new BoardField(From.x + 1, From.y);
				PieceCanMoveTo(TmpBoard, CurField, TmpBoard[From.x, From.y].Direction, PieceMoves);
				CurField = new BoardField(From.x - 1, From.y);
				PieceCanMoveTo(TmpBoard, CurField, TmpBoard[From.x, From.y].Direction, PieceMoves);
				break;
			default:
				break;
			}
			
			return PieceMoves;
		}
		
		private ValidMoves GetDropsForPiece(PieceType Piece, PieceDirection Direction)
		{
			ValidMoves PieceMoves = new ValidMoves();
			
			for (int x = 0; x < Game.BOARD_SIZE; x++)
			{
				for (int y = 0; y < Game.BOARD_SIZE; y++)
				{
					if (Board[x, y].Piece != PieceType.NONE)
						continue;
					
					if (Piece == PieceType.FUHYOU)
					{
						//check for 2 pawns in a column
						bool found = false;
						for (int i = 0; i < Game.BOARD_SIZE; i++)
						{
							if (Board[x, i].Piece == PieceType.FUHYOU && Board[x, i].Direction == Direction)
							{
								found = true;
								break;
							}
						}
						if (found)
						{
							continue;
						}
						
						//TODO check for mate with pawn
					}
					
					BoardField To = new BoardField(x, y);
					
					if (CheckForcedPromotion(To, Piece, Direction == PieceDirection.UP))
						continue;
					
					PieceMoves.Add(To);
				}
			}
			
			return PieceMoves;
		}
		
		private static bool IsInCheck(FieldInfo[,] TmpBoard, PieceDirection PlayerDirection)
		{
			for (int x = 0; x < Game.BOARD_SIZE; x++)
			{
				for (int y = 0; y < Game.BOARD_SIZE; y++)
				{
					if (TmpBoard[x, y].Piece != PieceType.NONE && TmpBoard[x, y].Direction != PlayerDirection)
					{
						foreach (BoardField AttackedField in GetMovesForPiece(TmpBoard, new BoardField(x, y)))
						{
							if (TmpBoard[AttackedField.x, AttackedField.y].Piece == PieceType.OUSHOU
								&& TmpBoard[AttackedField.x, AttackedField.y].Direction == PlayerDirection)
							{
								return true;
							}
						}
					}
				}
			}
			
			return false;
		}
		
		private void UpdateValidMoves()
		{
			lock (this)
			{
			Console.WriteLine("UpdateValidMoves enter: ThreadID" + System.Threading.Thread.CurrentThread.ManagedThreadId);
			//update oponents pieces and initialize moves
			//TODO remove this var and use CurPlayer directly
			PieceDirection CurPlayerDirection = CurPlayer;
			
			//bool InCheck = false;
			FieldInfo[,] TmpBoard = (FieldInfo[,])Board.Clone();
			// new FieldInfo[BOARD_SIZE, BOARD_SIZE];
			
			
			for (int x = 0; x < Game.BOARD_SIZE; x++)
			{
				for (int y = 0; y < Game.BOARD_SIZE; y++)
				{
					ValidBoardMoves[x, y] = new ValidMoves();
					if (Board[x, y].Piece != PieceType.NONE)
					{
						BoardField CurField = new BoardField(x, y);
						
						foreach (BoardField AttackedField in GetMovesForPiece(Board, CurField))
						{
							//can not move over own pieces
							if (Board[AttackedField.x, AttackedField.y].Piece != PieceType.NONE && Board[AttackedField.x, AttackedField.y].Direction == Board[x, y].Direction)
								continue;
							
							//TODO don't clone the whole board each time
							TmpBoard = (FieldInfo[,])Board.Clone();
							TmpBoard[AttackedField.x, AttackedField.y] = Board[x, y];
							TmpBoard[x, y].Piece = PieceType.NONE;
							
							if (Board[AttackedField.x, AttackedField.y].Piece == PieceType.OUSHOU && Board[AttackedField.x, AttackedField.y].Direction != Board[x, y].Direction
								|| !IsInCheck(TmpBoard, Board[x, y].Direction))
							{
								ValidBoardMoves[x, y].Add(AttackedField);
								
								/*
								if (Board[AttackedField.x, AttackedField.y].Piece == PieceType.OUSHOU 
									&& Board[AttackedField.x, AttackedField.y].Direction == CurPlayerDirection)
								{
									//InCheck = true;
								}
								*/
							}
							
							//TmpBoard[AttackedField.x, AttackedField.y].Piece = PieceType.NONE;
							//TmpBoard[x, y] = Board[x, y];
						}
					}
				}
			}

			for (int PlayerNr = 0; PlayerNr < Game.PLAYER_COUNT; PlayerNr++)
			{
				PieceDirection Direction = PlayerNr == 0 ? PieceDirection.UP : PieceDirection.DOWN;
				
				for (int i = 0; i < (int)PieceType.PIECE_TYPES_COUNT; i++)
				{
					ValidOnHandMoves[PlayerNr, i] = new ValidMoves();

					if (OnHandPieces[PlayerNr, i] == 0)
						continue;
					
					foreach (BoardField DropField in GetDropsForPiece((PieceType)i, Direction))
					{
						//TODO don't clone the whole board each time
						TmpBoard = (FieldInfo[,])Board.Clone();
						TmpBoard[DropField.x, DropField.y].Piece = (PieceType)i;
						TmpBoard[DropField.x, DropField.y].Direction = Direction;
						
						if (!IsInCheck(TmpBoard, Direction))
						{
							ValidOnHandMoves[PlayerNr, i].Add(DropField);
						}
						
						//TmpBoard[DropField.x, DropField.y] = Board[DropField.x, DropField.y];
					}
				}
			}
			
			bool CanMove = false;
			for (int x = 0; x < Game.BOARD_SIZE; x++)
			{
				for (int y = 0; y < Game.BOARD_SIZE; y++)
				{
					if (Board[x, y].Piece != PieceType.NONE 
						&& Board[x, y].Direction == CurPlayerDirection
						&& ValidBoardMoves[x, y].Count > 0)
					{
						System.Console.WriteLine(String.Format("CanMove: from {0}, {1} (count {2}, dir {3} == {4})", x, y, ValidBoardMoves[x, y].Count, Board[x, y].Direction.ToString(), CurPlayerDirection.ToString()));
						CanMove = true;
						break;
					}
				}
				
				if (CanMove)
					break;
			}
			
			if (!CanMove)
			{
				int CurPlayerNumber = GetPlayerNumber(CurPlayer);
				for (int i = 0; i < (int)PieceType.PIECE_TYPES_COUNT; i++)
				{
					if (ValidOnHandMoves[CurPlayerNumber, i].Count > 0)
					{
						System.Console.WriteLine(String.Format("CanMove: from on hand {0} (count {1})", ((PieceType)i).ToString(), ValidOnHandMoves[CurPlayerNumber, i].Count));
						CanMove = true;
						break;
					}
				}
			}
			
			if (!CanMove)
			{
				System.Console.WriteLine("can not move");
				_Mate = true;
			}
			else
			{
				_Mate = false;
			}
			
			Dirty = false;
			
			Console.WriteLine("UpdateValidMoves leave");
			}
		}
		
		public void SetDefaultPosition()
		{
			int x, y;
			
			for (x = 0; x < Game.BOARD_SIZE; x++)
				for (y = 0; y < Game.BOARD_SIZE / 2; y++)
				{
					Board[x, y].Direction = PieceDirection.DOWN;
					Board[x, y].Piece = PieceType.NONE;
				}
			for (x = 0; x < Game.BOARD_SIZE; x++)
				for (y = Game.BOARD_SIZE / 2; y < Game.BOARD_SIZE; y++)
				{
					Board[x, y].Direction = PieceDirection.UP;
					Board[x, y].Piece = PieceType.NONE;
				}
			
			//white pieces
			Board[0, 0].Piece = PieceType.KYOUSHA;
			Board[1, 0].Piece = PieceType.KEIMA;
			Board[2, 0].Piece = PieceType.GINSHOU;
			Board[3, 0].Piece = PieceType.KINSHOU;
			Board[4, 0].Piece = PieceType.OUSHOU;
			Board[5, 0].Piece = PieceType.KINSHOU;
			Board[6, 0].Piece = PieceType.GINSHOU;
			Board[7, 0].Piece = PieceType.KEIMA;
			Board[8, 0].Piece = PieceType.KYOUSHA;
			Board[1, 1].Piece = PieceType.KAKUGYOU;
			Board[7, 1].Piece = PieceType.HISHA;
			y = 2;
			for (x = 0; x < Game.BOARD_SIZE; x++)
			{
				Board[x, y].Piece = PieceType.FUHYOU;
			}
			
			//black pieces
			Board[0, 8].Piece = PieceType.KYOUSHA;
			Board[1, 8].Piece = PieceType.KEIMA;
			Board[2, 8].Piece = PieceType.GINSHOU;
			Board[3, 8].Piece = PieceType.KINSHOU;
			Board[4, 8].Piece = PieceType.OUSHOU;
			Board[5, 8].Piece = PieceType.KINSHOU;
			Board[6, 8].Piece = PieceType.GINSHOU;
			Board[7, 8].Piece = PieceType.KEIMA;
			Board[8, 8].Piece = PieceType.KYOUSHA;
			Board[1, 7].Piece = PieceType.HISHA;
			Board[7, 7].Piece = PieceType.KAKUGYOU;
			y = 6;
			for (x = 0; x < Game.BOARD_SIZE; x++)
			{
				Board[x, y].Piece = PieceType.FUHYOU;
			}
			
			for (int i = 0; i < (int)PieceType.PIECE_TYPES_COUNT; i++)
			{
				OnHandPieces[0, i] = 0;
				OnHandPieces[1, i] = 0;
			}
			
			Dirty = true;
		}

		public void DebugPrint()
		{
			for (int y = 0; y < 9; y++)
			{
				Console.Write("|");
				for (int x = 8; x >= 0; x--)
				{
					if (Board[x, y].Piece != PieceType.NONE)
					{
						Console.Write(CommonShogiNotationHelpers.GetPieceNamings()[(int)Board[x, y].Piece]);
					}
					else
					{
						Console.Write(" ");
					}
					Console.Write("|");
				}
				Console.Write(Environment.NewLine);
			}
		}
		
		private void AddOnHandPiece(PieceDirection dir, PieceType Piece)
		{
			OnHandPieces[GetPlayerNumber(dir), (int)Piece]++;
			Dirty = true;
		}

		private void RemoveOnHandPiece(PieceDirection dir, PieceType Piece)
		{
			OnHandPieces[GetPlayerNumber(dir), (int)Piece]--;
			Dirty = true;
		}
		
		private int GetPlayerNumber(PieceDirection dir)
		{
			return dir == PieceDirection.UP ? 0 : 1;
		}
	}
}

