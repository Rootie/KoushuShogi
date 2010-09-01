// 
//  ShogiGame.cs
//  
//  Author:
//       Gerhard Götz <rootie232@googlemail.com>
//  
//  Copyright (c) 2010 Gerhard Götz
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
    public class Game
    {
		public const int BOARD_SIZE = 9;
		public const int PLAYER_COUNT = 2;
		public static readonly Char[] VerticalNamings = { 'a', 'b', 'c', 'd', 'e', 'f', 'g', 'h', 'i' };
		public static readonly Char[] HorizontalNamings = { '1', '2', '3', '4', '5', '6', '7', '8', '9' };
		public static readonly Char[] PieceNamings = {
			' ', //NONE
			'P', //FUHYOU    Pawn
			' ', //TOKIN     Promoted Pawn
			'L', //KYOUSHA   Lance
			' ', //NARIKYOU  Promoted Lance
			'N', //KEIMA     Knight
			' ', //NARIKEI   Promoted Knight
			'S', //GINSHOU   Silver General
			' ', //NARIGIN   Promoted Silver
			'G', //KINSHOU   Gold General
			'B', //KAKUGYOU  Bishop
			' ', //RYUUMA    Promoted Bishop (Horse)
			'R', //HISHA     Rook
			' ', //RYUUOU    Promoted Rook (Dragon)
			' '  //OUSHOU    King
		};

		private GameState _gameState = GameState.Review;
		public GameState gameState
		{
			get { return _gameState; }
			private set
			{
				_gameState = value;
				if (GameStateChanged != null)
				{
					GameStateChanged(this, new EventArgs());
				}
			}
		}

		public String GameFinishedReason = String.Empty;

		public FieldInfo[,] Board = new FieldInfo[BOARD_SIZE, BOARD_SIZE];
		public int[,] OnHandPieces = new int[PLAYER_COUNT, (int)PieceType.PIECE_TYPES_COUNT];

		public System.Collections.Generic.List<ExtendedMove> Moves = new System.Collections.Generic.List<ExtendedMove>();

		public IPlayerEngine BlackPlayerEngine;
		public IPlayerEngine WhitePlayerEngine;
		private IPlayerEngine CurPlayerEngine;
		public Player BlackPlayer
		{
			get;
			private set;
		}
		public Player WhitePlayer
		{
			get;
			private set;
		}
		private Player _CurPlayer;
		public Player CurPlayer
		{
			get { return _CurPlayer; }
			set
			{
				_CurPlayer = value;
				OnCurPlayerChanged();
			}
		}

		public bool Mate
		{
			get;
			private set;
		}
		
		public LocalPlayerMoveState localPlayerMoveState;
		public Move LocalPlayerMove;

		public Game()
		{
			BlackPlayer = new Player();
			WhitePlayer = new Player();
		}
		
		//public methods
        public void StartGame()
        {
        	if (gameState == GameState.Playing)
        		EndGame();
   
        	ClearMoves();
   
        	//TODO maybe this should be asigned somewhere else
        	CurPlayerEngine = BlackPlayerEngine;
        	CurPlayer = BlackPlayer;
   
        	BlackPlayerEngine.MoveReady += HandleMoveReady;
        	BlackPlayerEngine.Resign += HandleResign;
        	WhitePlayerEngine.MoveReady += HandleMoveReady;
        	WhitePlayerEngine.Resign += HandleResign;
   
			if (BlackPlayerEngine is LocalPlayer)
        	{
        		(BlackPlayerEngine as LocalPlayer).NeedMove += HandleNeedMove;
        	}
        	if (WhitePlayerEngine is LocalPlayer)
        	{
        		(WhitePlayerEngine as LocalPlayer).NeedMove += HandleNeedMove;
        	}
   
			gameState = GameState.Playing;
        	WhitePlayerEngine.StartGame(false, Board, OnHandPieces);
        	BlackPlayerEngine.StartGame(true, Board, OnHandPieces);
        }
		
		public void EndGame()
		{
			EndGame("Game stopped by user");
		}
		
		public void EndGame(String Reason)
		{
			GameFinishedReason = Reason;
			gameState = GameState.Review;
			
			BlackPlayerEngine.MoveReady -= HandleMoveReady;
			BlackPlayerEngine.Resign -= HandleResign;
			WhitePlayerEngine.MoveReady -= HandleMoveReady;
			WhitePlayerEngine.Resign -= HandleResign;
			
			BlackPlayerEngine.EndGame();
			WhitePlayerEngine.EndGame();
		}
		
        public int CurrentPlayerNumber
		{
			get { return CurPlayer == BlackPlayer ? 0 : 1; }
		}

		public int GetPlayerNumber(Player player)
		{
			return player == BlackPlayer ? 0 : 1;
		}
#region user interaction
		public void FieldClicked(int x, int y)
		{
			System.Console.WriteLine(String.Format("Clicked on Field {0} {1}", x, y));
			
			switch (localPlayerMoveState)
			{
			case LocalPlayerMoveState.Wait:
				return;
			case LocalPlayerMoveState.PickSource:
				if (Board[x, y].Piece != PieceType.NONE && Board[x, y].Direction == (CurPlayer == BlackPlayer ? PieceDirection.UP : PieceDirection.DOWN))
				{
					LocalPlayerMove.OnHandPiece = PieceType.NONE;
					LocalPlayerMove.From.x = x;
					LocalPlayerMove.From.y = y;
					
					localPlayerMoveState = LocalPlayerMoveState.PickDestination;
				}
				break;
			case LocalPlayerMoveState.PickDestination:
				if (Board[x, y].Direction == ((CurPlayer == BlackPlayer) ? PieceDirection.UP : PieceDirection.DOWN)
				    && Board[x, y].Piece != PieceType.NONE)
				{
					localPlayerMoveState = LocalPlayerMoveState.PickSource;
					return;
				}
				
				LocalPlayerMove.To.x = x;
				LocalPlayerMove.To.y = y;
				
				Move NewMove = new Move(LocalPlayerMove);
				NewMove.To.x = x;
				NewMove.To.y = y;
				
				bool NormalMoveValid = IsMoveValid(NewMove);
				NewMove.promote = true;
				bool PromotedMoveValid = IsMoveValid(NewMove);

				if (NormalMoveValid && !PromotedMoveValid)
				{
					//move complete
					FinishLocalPlayerMove();
				}
				else if (NormalMoveValid && PromotedMoveValid)
				{
					localPlayerMoveState = LocalPlayerMoveState.PickPromotion;
				}
				else if (!NormalMoveValid && PromotedMoveValid)
				{
					//forced promotion. move complete
					LocalPlayerMove.promote = true;
					FinishLocalPlayerMove();
				}
				else
				{
					//move is not valid
					//TODO maybe raise some event to tell the user that the move is not valid
				}
				
				break;
			default:
				break;
			}
		}
		
		public void OnHandPieceClicked(PieceType piece)
		{
			System.Console.WriteLine(String.Format("Clicked on hand {0}", piece.ToString()));

			switch (localPlayerMoveState)
			{
			case LocalPlayerMoveState.Wait:
				return;
			case LocalPlayerMoveState.PickSource:
				{
					LocalPlayerMove.OnHandPiece = piece;
					localPlayerMoveState = LocalPlayerMoveState.PickDestination;
				}
				break;
			case LocalPlayerMoveState.PickDestination:
				{
					LocalPlayerMove.OnHandPiece = PieceType.NONE;
					localPlayerMoveState = LocalPlayerMoveState.PickSource;
				}
				break;
			default:
				break;
			}
		}
		
		public void PromotionClicked(bool promote)
		{
			//TODO switch (localPlayerMoveState)
			System.Console.WriteLine("Promotion choosen: " + promote.ToString());
			LocalPlayerMove.promote = promote;
			
			//move complete. send it to the player
			FinishLocalPlayerMove();
		}
		
		private void FinishLocalPlayerMove()
		{
			localPlayerMoveState = LocalPlayerMoveState.Wait;
			(CurPlayerEngine as LocalPlayer).MakeMove(LocalPlayerMove);
			LocalPlayerMove = new Move();
		}
		
		public void Undo()
		{
			if (Moves.Count < 1
				|| (!(BlackPlayerEngine is LocalPlayer) && !(WhitePlayerEngine is LocalPlayer)))
				return;
			
			//restore last move
			RestorePosition(Moves[Moves.Count - 1]);
			RemoveLastMove();
			
			if (!(BlackPlayerEngine is LocalPlayer && WhitePlayerEngine is LocalPlayer))
			{
				if (!(CurPlayerEngine is LocalPlayer))
				{
					//restore second to last move
					RestorePosition(Moves[Moves.Count - 1]);
					RemoveLastMove();
				}
				
				BlackPlayerEngine.Undo();
				WhitePlayerEngine.Undo();
			}

			if (CurPlayerEngine is LocalPlayer)
				localPlayerMoveState = LocalPlayerMoveState.PickSource;

			OnPiecesChanged();
		}
#endregion
		public void SetDefaultBoard()
		{
			int x, y;
			
			for (x = 0; x < BOARD_SIZE; x++)
				for (y = 0; y < BOARD_SIZE / 2; y++)
				{
					Board[x, y].Direction = PieceDirection.DOWN;
					Board[x, y].Piece = PieceType.NONE;
				}
			for (x = 0; x < BOARD_SIZE; x++)
				for (y = BOARD_SIZE / 2; y < BOARD_SIZE; y++)
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
			for (x = 0; x < BOARD_SIZE; x++)
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
			for (x = 0; x < BOARD_SIZE; x++)
			{
				Board[x, y].Piece = PieceType.FUHYOU;
			}
			
			for (int i = 0; i < (int)PieceType.PIECE_TYPES_COUNT; i++)
			{
				OnHandPieces[0, i] = 0;
				OnHandPieces[1, i] = 0;
			}
			
			OnPiecesChanged();
		}

		public Position GetCurPosition()
		{
			Position CurPosition;
			CurPosition.Board = (FieldInfo[,])Board.Clone();
			CurPosition.OnHandPieces = (int[,])OnHandPieces.Clone();
			CurPosition.CurPlayer = CurPlayer == BlackPlayer ? PieceDirection.UP : PieceDirection.DOWN;
			
			return CurPosition;
		}

		private void RestorePosition(ExtendedMove LastMove)
		{
			Board = (FieldInfo[,])LastMove.OriginalPosition.Board.Clone();
			OnHandPieces = (int[,])LastMove.OriginalPosition.OnHandPieces.Clone();
			CurPlayer = LastMove.OriginalPosition.CurPlayer == PieceDirection.UP ? BlackPlayer : WhitePlayer;
			CurPlayerEngine = LastMove.OriginalPosition.CurPlayer == PieceDirection.UP ? BlackPlayerEngine : WhitePlayerEngine;
		}
		
		private void AddMove(Move move)
		{
			ExtendedMove ExMove;
			ExMove.move = move;
			ExMove.OriginalPosition = GetCurPosition();
			
			Moves.Add(ExMove);
			
			//if a piece was captured add it to the own pieces on hand
			if (Board[move.To.x, move.To.y].Piece != PieceType.NONE)
			{
				AddOnHandPiece(CurPlayer, Board[move.To.x, move.To.y].Piece.GetUnpromotedPiece());
			}
			
			//check if the piece is from the hand or from the board
			if (move.OnHandPiece != PieceType.NONE)
			{
				Board[move.To.x, move.To.y].Piece = move.OnHandPiece;
				Board[move.To.x, move.To.y].Direction = CurPlayer == BlackPlayer ? PieceDirection.UP : PieceDirection.DOWN;
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
			
			OnPiecesChanged();
			OnMoveAdded(move);
		}
		
		private void RemoveLastMove()
		{
			Moves.RemoveAt(Moves.Count - 1);
			
			OnMoveRemoved();
		}

		private void ClearMoves()
		{
			Moves.Clear();
			
			OnMovesChanged();
		}

		private void AddOnHandPiece(Player player, PieceType Piece)
		{
			OnHandPieces[CurrentPlayerNumber, (int)Piece]++;
		}

		private void RemoveOnHandPiece(Player player, PieceType Piece)
		{
			OnHandPieces[CurrentPlayerNumber, (int)Piece]--;
		}

		private void SwitchPlayer()
		{
			if (CurPlayer == BlackPlayer)
			{
				CurPlayerEngine = WhitePlayerEngine;
				CurPlayer = WhitePlayer;
			}
			else
			{
				CurPlayerEngine = BlackPlayerEngine;
				CurPlayer = BlackPlayer;
			}
		}

#region move validation
		private ValidMoves[,] ValidBoardMoves = new ValidMoves[BOARD_SIZE ,BOARD_SIZE];
		private ValidMoves[,] ValidOnHandMoves = new ValidMoves[PLAYER_COUNT, (int)PieceType.PIECE_TYPES_COUNT];
		
		public ValidMoves GetValidBoardMoves(BoardField From)
		{
			return ValidBoardMoves[From.x, From.y];
			/*
			ValidMoves moves = new ValidMoves();
			for (int x = 0; x < BOARD_SIZE; x++)
			{
				for (int y = 0; y < BOARD_SIZE; y++)
				{
					BoardField curField = new BoardField(x, y);
					moves.Add(curField);
				}
			}
			return moves;*/
		}

		public ValidMoves GetValidOnHandMoves(PieceType piece, bool BlackPlayer)
		{
			return ValidOnHandMoves[BlackPlayer?0:1, (int)piece];
		}

		public bool IsMoveValid(Move move)
		{
			//validate move
			ValidMoves validMoves;
			
			if (move.OnHandPiece == PieceType.NONE)
			{
				if (Board[move.From.x, move.From.y].Direction != (CurPlayer == BlackPlayer ? PieceDirection.UP : PieceDirection.DOWN))
					return false;
				
				//TODO check (forced) promotion
				if (Board[move.From.x, move.From.y].Piece.CanPromote())
				{
					BoardField To = new BoardField(move.To.x, move.To.y);
					if (CheckForcedPromotion(To, Board[move.From.x, move.From.y].Piece, CurPlayer == BlackPlayer) && move.promote == false)
						return false;
					
					//these variables are turned as if seen from the
					//black player to ease checking
					int from_y_nor = move.From.y;
					int to_y_nor = move.To.y;
					if (CurPlayer == WhitePlayer)
					{
						from_y_nor = BOARD_SIZE - from_y_nor - 1;
						to_y_nor = BOARD_SIZE - to_y_nor - 1;
					}
				
					//no forced promotion, need to ask the player if he wants to promote
					if (!(to_y_nor <= 2 || from_y_nor <= 2) && move.promote == true)
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

				validMoves = GetValidOnHandMoves(move.OnHandPiece, CurPlayer == BlackPlayer);
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
			//these variable is turned as if seen from the
			//black player to ease checking
			int to_y_nor = To.y;
			if (!BlackPlayer)
			{
				to_y_nor = BOARD_SIZE - to_y_nor - 1;
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

		private static bool PieceCanMoveTo(FieldInfo[,] TmpBoard, BoardField To, PieceDirection Direction, ValidMoves PieceMoves)
		{
			//check board bounds
			if (To.x < 0 || To.x >= BOARD_SIZE || To.y < 0 || To.y >= BOARD_SIZE)
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
			
			for (int x = 0; x < BOARD_SIZE; x++)
			{
				for (int y = 0; y < BOARD_SIZE; y++)
				{
					if (Board[x, y].Piece != PieceType.NONE)
						continue;
					
					if (Piece == PieceType.FUHYOU)
					{
						//check for 2 pawns in a column
						bool found = false;
						for (int i = 0; i < BOARD_SIZE; i++)
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
			for (int x = 0; x < BOARD_SIZE; x++)
			{
				for (int y = 0; y < BOARD_SIZE; y++)
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
			//update oponents pieces and initialize moves
			PieceDirection CurPlayerDirection = CurPlayer == BlackPlayer ? PieceDirection.UP : PieceDirection.DOWN;
			
			//bool InCheck = false;
			FieldInfo[,] TmpBoard = (FieldInfo[,])Board.Clone();
			// new FieldInfo[BOARD_SIZE, BOARD_SIZE];
			
			
			for (int x = 0; x < BOARD_SIZE; x++)
			{
				for (int y = 0; y < BOARD_SIZE; y++)
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
								if (Board[AttackedField.x, AttackedField.y].Piece == PieceType.OUSHOU 
									&& Board[AttackedField.x, AttackedField.y].Direction == CurPlayerDirection)
								{
									//InCheck = true;
								}
							}
							
							//TmpBoard[AttackedField.x, AttackedField.y].Piece = PieceType.NONE;
							//TmpBoard[x, y] = Board[x, y];
						}
					}
				}
			}

			for (int PlayerNr = 0; PlayerNr < PLAYER_COUNT; PlayerNr++)
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
			for (int x = 0; x < BOARD_SIZE; x++)
			{
				for (int y = 0; y < BOARD_SIZE; y++)
				{
					if (Board[x, y].Piece != PieceType.NONE 
						&& Board[x, y].Direction != CurPlayerDirection
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
				int OponentPlayerNr = CurPlayer == BlackPlayer ? 1 : 0;
				for (int i = 0; i < (int)PieceType.PIECE_TYPES_COUNT; i++)
				{
					if (ValidOnHandMoves[OponentPlayerNr, i].Count > 0)
					{
						System.Console.WriteLine(String.Format("CanMove: from on hand {0} (count {1}, dir {2} == {3})", ((PieceType)i).ToString(), ValidOnHandMoves[OponentPlayerNr, i].Count, OponentPlayerNr, CurrentPlayerNumber));
						CanMove = true;
						break;
					}
				}
			
			}
			
			if (!CanMove)
			{
				System.Console.WriteLine("can not move");
				Mate = true;
			}
			else
			{
				Mate = false;
			}
			
		}
#endregion
		
		//event handlers
		private void HandleResign(object sender, ResignEventArgs e)
		{
			if (gameState != GameState.Playing)
				return;
			if (sender != CurPlayerEngine)
				return;
			
			GameFinishedReason = e.Message;
			gameState = GameState.Review;
			
        	BlackPlayerEngine.EndGame();
        	WhitePlayerEngine.EndGame();
        }

        private void HandleMoveReady(Object sender, MoveReadyEventArgs e)
        {
        	if (sender != CurPlayerEngine)
        		return;
   
        	if (IsMoveValid(e.move))
        	{
        		AddMove(e.move);
    
				System.Console.WriteLine("cheking for mate");
        		if (Mate)
        		{
					//send last move to opponent
        			if (CurPlayer == BlackPlayer)
        			{
        				WhitePlayerEngine.OponentMove(e.move);
        			}
					else
        			{
        				BlackPlayerEngine.OponentMove(e.move);
					}
        			System.Console.WriteLine("mate");
        			EndGame("Mate");
        			return;
        		}
        		//TODO check for sennichite

				System.Console.WriteLine("no mate");
        		SwitchPlayer();
        		CurPlayerEngine.OponentMove(e.move);
        	}
        	else
        	{
				System.Console.WriteLine("illegal move: " + e.move.ToString());
        		EndGame("illegal move: " + e.move.ToString());
			}
        }

		private void HandleNeedMove(object sender, EventArgs e)
		{
			localPlayerMoveState = LocalPlayerMoveState.PickSource;
		}

		//events
		protected void OnPiecesChanged()
		{
			UpdateValidMoves();
			
			if (PiecesChanged != null)
			{
				PiecesChanged(this, new EventArgs());
			}
		}
		protected void OnCurPlayerChanged()
		{
			if (CurPlayerChanged != null)
			{
				CurPlayerChanged(this, new EventArgs());
			}
		}
		protected void OnMoveAdded(Move move)
		{
			if (MoveAdded != null)
			{
				MoveAdded(this, new MoveAddedEventArgs(move));
			}
		}
		protected void OnMoveRemoved()
		{
			if (MoveRemoved != null)
			{
				MoveRemoved(this, new EventArgs());
			}
		}
		protected void OnMovesChanged()
		{
			if (MovesChanged != null)
			{
				MovesChanged(this, new EventArgs());
			}
		}

		public event EventHandler<MoveAddedEventArgs> MoveAdded;
		public event EventHandler MoveRemoved;
		public event EventHandler MovesChanged;
		public event EventHandler PiecesChanged;
		public event EventHandler CurPlayerChanged;
		public event EventHandler GameStateChanged;
	}
	
	[Serializable]
	public sealed class MoveAddedEventArgs : EventArgs
	{
		public Move move;
		public MoveAddedEventArgs(Move move)
		{
			this.move = move;
		}
	}
	
	public enum PieceType
	{
		NONE,
		//Pawn
		FUHYOU,
		//Promoted Pawn
		TOKIN,
		//Lance
		KYOUSHA,
		//Promoted Lance
		NARIKYOU,
		//Knight
		KEIMA,
		//Promoted Knight
		NARIKEI,
		//Silver General
		GINSHOU,
		//Promoted Silver
		NARIGIN,
		//Gold General
		KINSHOU,
		//Bishop
		KAKUGYOU,
		//Promoted Bishop (Horse)
		RYUUMA,
		//Rook
		HISHA,
		//Promoted Rook (Dragon)
		RYUUOU,
		//King
		OUSHOU,
		PIECE_TYPES_COUNT
	}

	public static class PieceTypeExtensions
	{
		public static PieceType GetUnpromotedPiece(this PieceType Piece)
		{
			switch (Piece)
			{
			case PieceType.TOKIN:
				return PieceType.FUHYOU;
			case PieceType.NARIKYOU:
				return PieceType.KYOUSHA;
			case PieceType.NARIKEI:
				return PieceType.KEIMA;
			case PieceType.NARIGIN:
				return PieceType.GINSHOU;
			case PieceType.RYUUMA:
				return PieceType.KAKUGYOU;
			case PieceType.RYUUOU:
				return PieceType.HISHA;
			}
			
			return Piece;
		}

		public static PieceType GetPromotedPiece(this PieceType Piece)
		{
			switch (Piece)
			{
			case PieceType.FUHYOU:
				return PieceType.TOKIN;
			case PieceType.KYOUSHA:
				return PieceType.NARIKYOU;
			case PieceType.KEIMA:
				return PieceType.NARIKEI;
			case PieceType.GINSHOU:
				return PieceType.NARIGIN;
			case PieceType.KAKUGYOU:
				return PieceType.RYUUMA;
			case PieceType.HISHA:
				return PieceType.RYUUOU;
			}
			
			return Piece;
		}
		
		public static bool IsPromoted(this PieceType Piece)
		{
			switch (Piece)
			{
			case PieceType.TOKIN:
			case PieceType.NARIKYOU:
			case PieceType.NARIKEI:
			case PieceType.NARIGIN:
			case PieceType.RYUUMA:
			case PieceType.RYUUOU:
				return true;
			}
			
			return false;
		}
		
		public static bool CanPromote(this PieceType Piece)
		{
			return Piece != Piece.GetPromotedPiece();
		}
	}

	public enum PieceDirection
	{
		UP,
		DOWN
	}

	public struct FieldInfo
	{
		public PieceType Piece;
		public PieceDirection Direction;
	}

	public struct BoardField
	{
		public int x;
		public int y;
		
		public BoardField(int x, int y)
		{
			this.x = x;
			this.y = y;
		}
		public override bool Equals(object obj)
		{
		      if (obj == null || GetType() != obj.GetType()) return false;
		      BoardField f = (BoardField)obj;
		      return (x == f.x) && (y == f.y);
		}
		
		public override int GetHashCode()
		{
			return x ^ y;
		}
	}
	
	public struct Position
	{
		public FieldInfo[,] Board;
		public int[,] OnHandPieces;
		public PieceDirection CurPlayer;
	}
	
	public class ValidMoves : System.Collections.Generic.List<BoardField>
	{
		public ValidMoves() : base(Game.BOARD_SIZE*Game.BOARD_SIZE) {}
	}
	
	public struct Move
	{
		public PieceType OnHandPiece;
		public BoardField From;
		public BoardField To;

		public bool promote;

		public Move(Move src)
		{
			OnHandPiece = src.OnHandPiece;
			From.x = src.From.x;
			From.y = src.From.y;
			To.x = src.To.x;
			To.y = src.To.y;
			promote = src.promote;
		}
		
		public override String ToString()
		{
			String s = String.Empty;
			if (OnHandPiece != PieceType.NONE)
			{
				s += Game.PieceNamings[(int)OnHandPiece];
				s += '*';
			}
			else
			{
				s += Game.HorizontalNamings[From.x];
				s += Game.VerticalNamings[From.y];
			}
			
			s += Game.HorizontalNamings[To.x];
			s += Game.VerticalNamings[To.y];
			
			if (promote)
			{
				s += "+";
			}
			
			return s;
		}
	}
	
	public struct ExtendedMove
	{
		public Move move;
		public Position OriginalPosition;
	}

	public enum GameState
	{
		Review,
		Playing
	}

	public enum LocalPlayerMoveState
	{
		Wait,
		PickSource,
		PickDestination,
		PickPromotion
	};
}
