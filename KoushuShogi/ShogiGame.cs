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
		public static readonly int BOARD_SIZE = 9;
		public static readonly int PLAYER_COUNT = 2;

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

		private String _GameFinishedReason = String.Empty;
		public String GameFinishedReason
		{
			get
			{
				return _GameFinishedReason;
			}
			private set
			{
				_GameFinishedReason = value;
			}
		}

		private Position _Position = new Position();
		public Position Position
		{
			get
			{
				return _Position.Clone();
			}
			set
			{
				if (gameState == GameState.Playing)
					throw new NotSupportedException("Can not set position while playing");
				
				_Position = value.Clone();
			}
		}

		//TODO restrict access
		public System.Collections.Generic.List<ExtendedMove> Moves = new System.Collections.Generic.List<ExtendedMove>();

		private TimeSpan _GameTime;
		//negative time means infinite thinking time
		public TimeSpan GameTime
		{
			get
			{
				return _GameTime;
			}
			set
			{
				if (gameState == GameState.Playing)
					throw new NotSupportedException("Can not set times while playing");
				
				_GameTime = value;
			}
		}
		private TimeSpan _ByouYomiTime;
		public TimeSpan ByouYomiTime
		{
			get
			{
				return _ByouYomiTime;
			}
			set
			{
				if (gameState == GameState.Playing)
					throw new NotSupportedException("Can not set times while playing");
				
				if (value.Ticks <= 0)
					throw new ArgumentException("Value must be positive", "ByouYomiTime");
				
				_ByouYomiTime = value;
			}
		}
		private TimeSpan BlackTime;
		private bool BlackIsInByouYomi;
		private TimeSpan WhiteTime;
		private bool WhiteIsInByouYomi;
		private DateTime TurnStartTime;
		System.Timers.Timer GameTimeOut = new System.Timers.Timer();
		
		private IPlayerEngine _BlackPlayerEngine;
		public IPlayerEngine BlackPlayerEngine
		{
			get { return _BlackPlayerEngine; }
			set
			{
				SetPlayerEngine(ref _BlackPlayerEngine, value);
			}
		}
		private IPlayerEngine _WhitePlayerEngine;
		public IPlayerEngine WhitePlayerEngine
		{
			get { return _WhitePlayerEngine; }
			set 
			{
				SetPlayerEngine(ref _WhitePlayerEngine, value);
			}
		}
		private IPlayerEngine CurPlayerEngine
		{
			get { return _Position.CurPlayer == PieceDirection.UP ? BlackPlayerEngine : WhitePlayerEngine; }
		}
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
		public Player CurPlayer
		{
			get { return _Position.CurPlayer == PieceDirection.UP ? BlackPlayer : WhitePlayer; }
		}

		public bool Mate
		{
			get;
			private set;
		}
		
		public LocalPlayerMoveState localPlayerMoveState { get; private set;}
		private Move _LocalPlayerMove;
		public Move GetLocalPlayerMove()
		{
			return _LocalPlayerMove;
		}

		//ctor
		public Game()
		{
			BlackPlayer = new Player();
			WhitePlayer = new Player();
			GameTimeOut.Elapsed += HandleGameTimeOutElapsed;
		}

		//public methods
        public void StartGame(Player StartingPlayer)
        {
        	if (BlackPlayerEngine == null || WhitePlayerEngine == null)
        	{
        		throw new ArgumentNullException("PlayerEngine", "BlackPlayerEngine and WhitePlayerEngine must be set when starting a game");
        	}
   
        	if (gameState == GameState.Playing)
        		EndGame();
   
        	ClearMoves();
   
        	if (StartingPlayer == BlackPlayer)
        	{
        		_Position.CurPlayer = PieceDirection.UP;
        	}
        	else if (StartingPlayer == WhitePlayer)
        	{
        		_Position.CurPlayer = PieceDirection.DOWN;
        	}
        	else
        	{
        		throw new ArgumentException("StartingPlayer must be set to one of BlackPlayer or WhitePlayer", "StartingPlayer");
        	}
   
			BlackTime = GameTime;
        	WhiteTime = GameTime;
        	BlackIsInByouYomi = false;
        	WhiteIsInByouYomi = false;
   
			gameState = GameState.Playing;
        	TurnStartTime = DateTime.Now;
        	if (GameTime.Ticks > 0)
        	{
        		GameTimeOut.Interval = GameTime.TotalMilliseconds;
        		GameTimeOut.Enabled = true;
        	}
        	BlackPlayerEngine.StartGame(true, this.Position);
        	WhitePlayerEngine.StartGame(false, this.Position);
			
			Console.WriteLine("Game Position Hash: " + _Position.GetHashCode().ToString());
        }
		
		public void EndGame()
		{
			EndGame("Game stopped by user");
		}
		
		public void EndGame(String Reason)
		{
			if (gameState != GameState.Playing)
				return;
			
			GameFinishedReason = Reason;
			gameState = GameState.Review;
			
			UpdateGameTimes();
			
			BlackPlayerEngine.EndGame();
			WhitePlayerEngine.EndGame();
		}
		
		public void LoadSaveGame(SaveGame savegame)
		{
			if (savegame.BlackPlayer == null)
			{
				throw new Exception("BlackPlayer may not be null");
			}
			if (savegame.WhitePlayer == null)
			{
				throw new Exception("WhitePlayer may not be null");
			}
			if (savegame.BlackPlayer == savegame.WhitePlayer)
			{
				throw new Exception("BlackPlayer and WhitePlayer must differ");
			}
			
			if (gameState == GameState.Playing)
				EndGame();
			
			BlackPlayer = savegame.BlackPlayer;
			WhitePlayer = savegame.WhitePlayer;
			
			ClearMoves();
			
			Position CurPosition = savegame.StartingPosition;
			
			foreach (Move move in savegame.Moves)
			{
				ExtendedMove exMove = new ExtendedMove();
				exMove.move = move;
				exMove.OriginalPosition = CurPosition.Clone();
				if (!CurPosition.ApplyMove(move))
				{
					throw new Exception("Invalid move");
				}
				Moves.Add(exMove);
			}
			
			_Position = CurPosition;
			
			OnPositionChanged();
			OnMovesChanged();
		}
		
		public void GetRemainingTimes(out TimeSpan Black, out TimeSpan White)
		{
			Black = BlackTime;
			if (gameState == GameState.Playing && CurPlayer == BlackPlayer && GameTime.Ticks > 0)
			{
				Black -= DateTime.Now.Subtract(TurnStartTime);
			}
			
			White = WhiteTime;
			if (gameState == GameState.Playing && CurPlayer == WhitePlayer && GameTime.Ticks > 0)
			{
				White -= DateTime.Now.Subtract(TurnStartTime);
			}
		}

#region user interaction
		public void FieldClicked(int x, int y)
		{
			System.Console.WriteLine(String.Format("Clicked on Field {0} {1}", x, y));
			
			if (!(CurPlayerEngine is LocalPlayer))
				return;
			
			switch (localPlayerMoveState)
			{
			case LocalPlayerMoveState.Wait:
				return;
			case LocalPlayerMoveState.PickSource:
				if (_Position.Board[x, y].Piece != PieceType.NONE 
					&& _Position.Board[x, y].Direction == _Position.CurPlayer)
				{
					_LocalPlayerMove.OnHandPiece = PieceType.NONE;
					_LocalPlayerMove.From.x = x;
					_LocalPlayerMove.From.y = y;
					
					localPlayerMoveState = LocalPlayerMoveState.PickDestination;
				}
				break;
			case LocalPlayerMoveState.PickDestination:
				if (_Position.Board[x, y].Piece != PieceType.NONE
				    && _Position.Board[x, y].Direction == _Position.CurPlayer)
				{
					localPlayerMoveState = LocalPlayerMoveState.PickSource;
					return;
				}
				
				_LocalPlayerMove.To.x = x;
				_LocalPlayerMove.To.y = y;
				
				Move NewMove = new Move(_LocalPlayerMove);
				NewMove.To.x = x;
				NewMove.To.y = y;
				
				bool NormalMoveValid = _Position.IsMoveValid(NewMove);
				NewMove.promote = true;
				bool PromotedMoveValid = _Position.IsMoveValid(NewMove);

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
					_LocalPlayerMove.promote = true;
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

			if (!(CurPlayerEngine is LocalPlayer))
				return;
			
			switch (localPlayerMoveState)
			{
			case LocalPlayerMoveState.Wait:
				return;
			case LocalPlayerMoveState.PickSource:
				{
					_LocalPlayerMove.OnHandPiece = piece;
					localPlayerMoveState = LocalPlayerMoveState.PickDestination;
				}
				break;
			case LocalPlayerMoveState.PickDestination:
				{
					_LocalPlayerMove.OnHandPiece = PieceType.NONE;
					localPlayerMoveState = LocalPlayerMoveState.PickSource;
				}
				break;
			default:
				break;
			}
		}
		
		public void PromotionClicked(bool promote)
		{
			System.Console.WriteLine("Promotion choosen: " + promote.ToString());
			
			if (localPlayerMoveState != LocalPlayerMoveState.PickPromotion)
				return;
			
			_LocalPlayerMove.promote = promote;
			
			//move complete. send it to the player
			FinishLocalPlayerMove();
		}
		
		private void FinishLocalPlayerMove()
		{
			localPlayerMoveState = LocalPlayerMoveState.Wait;
			(CurPlayerEngine as LocalPlayer).MakeMove(_LocalPlayerMove);
			_LocalPlayerMove = new Move();
		}
		
		public void Undo()
		{
			if (Moves.Count < 1
				|| (!(BlackPlayerEngine is LocalPlayer) && !(WhitePlayerEngine is LocalPlayer)))
				return;
			
			//restore last move
			UpdateGameTimes();
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

			OnPositionChanged();
		}
#endregion
		//private methods
		private void RestorePosition(ExtendedMove LastMove)
		{
			_Position = LastMove.OriginalPosition.Clone();
		}
		
		private void AddMove(Position OriginalPosition, Move move)
		{
			ExtendedMove ExMove;
			ExMove.move = move;
			ExMove.OriginalPosition = OriginalPosition;
			
			Moves.Add(ExMove);
			
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

		private void SetPlayerEngine(ref IPlayerEngine CurEngine, IPlayerEngine NewEngine)
		{
			if (gameState == GameState.Playing)
			{
				throw new InvalidOperationException("Can not set player engine while playing");
			}
			
			if (CurEngine != null)
			{
				CurEngine.MoveReady -= HandleMoveReady;
				CurEngine.Resign -= HandleResign;
			}
			
			CurEngine = NewEngine;
			
        	CurEngine.MoveReady += HandleMoveReady;
			CurEngine.Resign += HandleResign;
			
			if (CurEngine is LocalPlayer)
	    	{
	    		(CurEngine as LocalPlayer).NeedMove += HandleNeedMove;
	    	}
		}
				
		private void UpdateGameTimes()
		{
			if (GameTime.Ticks <= 0)
				return;
			
			if (CurPlayer == BlackPlayer)
			{
				if (BlackIsInByouYomi)
				{
					BlackTime = ByouYomiTime;
				}
				else
				{
					BlackTime -= DateTime.Now.Subtract(TurnStartTime);
				}
			}
			else
			{
				if (WhiteIsInByouYomi)
				{
					WhiteTime = ByouYomiTime;
				}
				else
				{
					WhiteTime -= DateTime.Now.Subtract(TurnStartTime);
				}
			}
			
        	TurnStartTime = DateTime.Now;
		}
		
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
        	if (gameState != GameState.Playing)
        		return;
        	if (sender != CurPlayerEngine)
        		return;
      
			GameTimeOut.Enabled = false;
        	UpdateGameTimes();
   
			Position OriginalPosition = this.Position;
      
			_Position.DebugPrint();
	       	if (_Position.ApplyMove(e.move))
        	{
				_Position.DebugPrint();
        		AddMove(OriginalPosition, e.move);
        		OnPositionChanged();
				
				System.Console.WriteLine("checking for mate");
        		if (_Position.Mate)
        		{
        			//send last move to opponent
        			if (CurPlayer == BlackPlayer)
        			{
        				BlackPlayerEngine.OponentMove(e.move);
        			}
        			else
        			{
        				WhitePlayerEngine.OponentMove(e.move);
        			}
        			System.Console.WriteLine("mate");
        			EndGame("Mate");
        			return;
        		}
        		//TODO check for sennichite

				System.Console.WriteLine("no mate");
    
        		if (GameTime.Ticks > 0)
        		{
        			GameTimeOut.Interval = CurPlayer == BlackPlayer ? BlackTime.TotalMilliseconds : WhiteTime.TotalMilliseconds;
        			GameTimeOut.Enabled = true;
				}
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
			if (gameState != GameState.Playing)
				return;
        	if (sender != CurPlayerEngine)
        		return;
			
			localPlayerMoveState = LocalPlayerMoveState.PickSource;
		}

		void HandleGameTimeOutElapsed(object sender, System.Timers.ElapsedEventArgs e)
		{
			Console.WriteLine("Timout");
			GameTimeOut.Enabled = false;
			if (CurPlayer == BlackPlayer)
			{
				if (BlackIsInByouYomi)
				{
					EndGame("Timeout");
				}
				else
				{
					BlackIsInByouYomi = true;
					BlackTime = ByouYomiTime;
					TurnStartTime = DateTime.Now;
					GameTimeOut.Interval = BlackTime.TotalMilliseconds;
					GameTimeOut.Enabled = true;
				}
			}
			else
			{
				if (WhiteIsInByouYomi)
				{
					EndGame("Timeout");
				}
				else
				{
					WhiteIsInByouYomi = true;
					WhiteTime = ByouYomiTime;
					TurnStartTime = DateTime.Now;
					GameTimeOut.Interval = WhiteTime.TotalMilliseconds;
					GameTimeOut.Enabled = true;
				}
			}
		}
		
		//events
		protected void OnPositionChanged()
		{
			Console.WriteLine ("ShogiGame: OnPositionChanged");
			if (PositionChanged != null)
			{
				PositionChanged(this, new EventArgs());
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
		public event EventHandler PositionChanged;
		public event EventHandler GameStateChanged;
	}
	
	[Serializable]
	public sealed class MoveAddedEventArgs : EventArgs
	{
		public Move move { get; private set;}
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
		
		public override string ToString ()
		{
			return Piece.ToString() + " " + Direction.ToString();
		}
	}

	public struct BoardField : IEquatable<BoardField>
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
			if (obj == null || GetType() != obj.GetType())
				return false;
			BoardField f = (BoardField)obj;
			return (x == f.x) && (y == f.y);
		}
		
		public bool Equals(BoardField Field)
		{
			return (x == Field.x) && (y == Field.y);
		}
		
		public override int GetHashCode()
		{
			return x ^ y;
		}
		
	    public static bool operator ==(BoardField a, BoardField b)
	    {
	    	return a.Equals(b);
	    }
		
	    public static bool operator !=(BoardField a, BoardField b)
	    {
	    	return !a.Equals(b);
		}
	}
	
	public class ValidMoves : System.Collections.Generic.List<BoardField>
	{
		public ValidMoves() : base(Game.BOARD_SIZE*Game.BOARD_SIZE) {}
	}
	
	public struct Move
	{
		private static readonly Char[] VerticalNamings = CommonShogiNotationHelpers.GetVerticalNamings();
		private static readonly Char[] HorizontalNamings = CommonShogiNotationHelpers.GetHorizontalNamings();
		private static readonly Char[] PieceNamings = CommonShogiNotationHelpers.GetPieceNamings();
		
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
				s += PieceNamings[(int)OnHandPiece];
				s += '*';
			}
			else
			{
				s += HorizontalNamings[From.x];
				s += VerticalNamings[From.y];
			}
			
			s += HorizontalNamings[To.x];
			s += VerticalNamings[To.y];
			
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
	
	public class SaveGame
	{
		public Player BlackPlayer = new Player();
		public Player WhitePlayer = new Player();
		
		public Position StartingPosition;
		
		public System.Collections.Generic.List<Move> Moves = new System.Collections.Generic.List<Move>();
	}
}
