// 
//  GnuShogiPlayer.cs
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
using System.Threading;

namespace Shogiban
{
	public class GnuShogiPlayer : IPlayerEngine
	{
		private static readonly Char[] VerticalNamings = {'a', 'b', 'c', 'd', 'e', 'f', 'g', 'h', 'i'};
		private static readonly Char[] HorizontalNamings = {'1', '2', '3', '4', '5', '6', '7', '8', '9'};
		private static readonly Char[] PieceNamings = {
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
			'k'  //OUSHOU    King
		};

		private bool Playing = false;

		private System.Diagnostics.Process ShogiProc;
		private Semaphore ReadSem = new Semaphore(0, int.MaxValue);

		#region Player implementation
		public GnuShogiPlayer()
		{
			ShogiProc = new System.Diagnostics.Process();
			ShogiProc.StartInfo.FileName = "gnushogi";
			ShogiProc.StartInfo.UseShellExecute = false;
			ShogiProc.StartInfo.RedirectStandardError = true;
			ShogiProc.StartInfo.RedirectStandardInput = true;
			ShogiProc.StartInfo.RedirectStandardOutput = true;
			
			ShogiProc.OutputDataReceived += HandleShogiProcOutputDataReceived;
			
			ShogiProc.Start();
			ShogiProc.BeginOutputReadLine();
			
			//read welcome message
			ReadFromShogiProc();
		}
		
		~GnuShogiPlayer()
		{
			Dispose();
		}

		public void StartGame(bool Blackplayer, FieldInfo[,] Board, int[,] OnHandPieces)
		{
			SetPosition(Board, OnHandPieces);
			
			if (Blackplayer)
			{
				SendToShogiProc("switch");
				ReadNextMove();
			}
			Playing = true;
		}
		
		public void EndGame()
		{
			Playing = false;

			SendToShogiProc("new");
		}

		private void SetPosition(FieldInfo[,] Board, int[,] OnHandPieces)
		{
			SendToShogiProc("edit");
			ReadFromShogiProc();
			ReadFromShogiProc();
			ReadFromShogiProc();
			ReadFromShogiProc();
			//clear the board
			SendToShogiProc("#");

			//black pieces
			SetPositionForPlayer(Board, OnHandPieces, true);
			
			//white pieces
			SendToShogiProc("c");
			SetPositionForPlayer(Board, OnHandPieces, false);

			//end editing
			SendToShogiProc(".");
//			SendToShogiProc("bd");
		}

		public void OponentMove(Move move)
		{
			if (!Playing)
				return;
			
			String MoveStr = String.Empty;
			
			if (move.OnHandPiece != PieceType.NONE)
			{
				MoveStr += PieceNamings[(int)move.OnHandPiece];
				MoveStr += '*';
			}
			else
			{
				MoveStr += HorizontalNamings[move.From.x];
				MoveStr += VerticalNamings[move.From.y];
			}
			
			MoveStr += HorizontalNamings[move.To.x];
			MoveStr += VerticalNamings[move.To.y];
			
			if (move.promote)
			{
				MoveStr += '+';
			}
			
			SendToShogiProc(MoveStr);

			//read output from move back
			String output = ReadFromShogiProc();
			//TODO validate output
			if (output.Contains("Illegal") && Resign != null)
			{
				Resign(this, new ResignEventArgs(output));
				return;
			}
			
			//retrieve next move
			ReadNextMove();
		}

		private PieceType GetPieceByName(Char Name)
		{
			for (int i = 0; i < PieceNamings.Length; i++)
			{
				if (Name == PieceNamings[i])
				{
					return (PieceType)i;
				}
			}
			
			throw new Exception("Invalid piece type '" + Name + "'."); //TODO make specific exception
		}

		private int GetColByName(Char Name)
		{
			for (int i = 0; i < HorizontalNamings.Length; i++)
			{
				if (Name == HorizontalNamings[i])
				{
					return i;
				}
			}
			
			throw new Exception("Invalid column '" + Name + "'."); //TODO make specific exception
		}

		private int GetRowByName(Char Name)
		{
			for (int i = 0; i < VerticalNamings.Length; i++)
			{
				if (Name == VerticalNamings[i])
				{
					return i;
				}
			}
			
			throw new Exception("Invalid row '" + Name + "'."); //TODO make specific exception
		}

		private Move MoveFromString(String MoveStr)
		{
			Move move = new Move();
			
			if (MoveStr[1] == '*')
			{
				move.OnHandPiece = GetPieceByName(MoveStr[0]);
			}
			else
			{
				move.From.x = GetColByName(MoveStr[0]);
				move.From.y = GetRowByName(MoveStr[1]);
			}
			
			move.To.x = GetColByName(MoveStr[2]);
			move.To.y = GetRowByName(MoveStr[3]);
			
			move.promote = MoveStr.Contains("+");
			
			return move;
		}

		public string Name
		{
			get;
			set;
		}
		
		public event EventHandler<MoveReadyEventArgs> MoveReady;
		public event EventHandler<ResignEventArgs> Resign;
		#endregion

		#region IDisposable implementation
		public void Dispose ()
		{
			SendToShogiProc("quit");
			ShogiProc.Close ();
		}
		#endregion

		private void SetPositionForPlayer(FieldInfo[,] Board, int[,] OnHandPieces, bool BlackPlayer)
		{
			PieceDirection Direction = BlackPlayer ? PieceDirection.UP : PieceDirection.DOWN;
			int PlayerNr = BlackPlayer ? 0 : 1;
			
			//board pieces
			for (int x = 0; x < Game.BOARD_SIZE; x++)
			{
				for (int y = 0; y < Game.BOARD_SIZE; y++)
				{
					if (!(Board[x, y].Piece != PieceType.NONE && Board[x, y].Direction == Direction))
						continue;
					String FieldStr = String.Empty;
					FieldStr += PieceNamings[(int)Board[x, y].Piece.GetUnpromotedPiece()];
					FieldStr += HorizontalNamings[x];
					FieldStr += VerticalNamings[y];
					if (Board[x, y].Piece.IsPromoted())
						FieldStr += "+";
					
					SendToShogiProc(FieldStr);
				}
			}
			
			//on hand pieces
			for (int Piece = 0; Piece < (int)PieceType.PIECE_TYPES_COUNT; Piece++)
			{
				for (int i = 0; i < OnHandPieces[PlayerNr, Piece]; i++)
				{
					String PieceStr = String.Empty;
					PieceStr += PieceNamings[i];
					PieceStr += "*";
					
					SendToShogiProc(PieceStr);
				}
			}
		}
		Thread ReadNextMoveThread;
		private void ReadNextMove()
		{
			ReadNextMoveThread = new Thread(new ThreadStart(ReadNextMoveThreadFunc));
			ReadNextMoveThread.IsBackground = true;
			ReadNextMoveThread.Start();
		}

		private void ReadNextMoveThreadFunc()
		{
			String output = ReadFromShogiProc();
			
			//if (output.Contains("mate"))
			//{
			//	return;
			//}
			
			try
			{
				int StartIdx = output.IndexOf(".. ");
			
				if (StartIdx < 0)
				{
					throw new Exception("No move found. (" + output + ")");
				}
			
				Move move = MoveFromString(output.Substring(StartIdx + 3));
			
				if (MoveReady != null)
				{
					MoveReady(this, new MoveReadyEventArgs(move));
				}
			}
			catch (Exception ex)
			{
				OnResign(ex.Message);
				return;
			}
			
			//check if there is a mate message
			System.Threading.Thread.Sleep(100);
			if (StdOutLines.Count > 0)
			{
				Playing = false;
				ReadFromShogiProc();
			}
		}
			
		private void SendToShogiProc(String cmd)
		{
			ShogiProc.StandardInput.WriteLine (cmd);
#if DEBUG
			System.Console.WriteLine(Name + " -> " + cmd);
#endif
		}
		
		private String ReadFromShogiProc()
		{
			String output;
			
			ReadSem.WaitOne();
			if (StdOutLines.Count > 0)
			{
				output = StdOutLines[0];
				StdOutLines.RemoveAt(0);
			}
			else
			{
				//output = ShogiProc.StandardOutput.ReadLine();
				output = "";
			}
			
#if DEBUG
			System.Console.WriteLine(Name + " <- " + output);
#endif
			return output;
		}
		
		private System.Collections.Generic.List<String> StdOutLines = new System.Collections.Generic.List<String>();
		
		private void HandleShogiProcOutputDataReceived(object sender, System.Diagnostics.DataReceivedEventArgs e)
		{
#if DEBUG
			System.Console.WriteLine(Name + " <- " + e.Data + " (Handler)");
#endif
			StdOutLines.Add(e.Data);
			ReadSem.Release();
			//new MessageDialog (this, DialogFlags.DestroyWithParent, MessageType.Info, ButtonsType.Ok, e.Data).Show ();
		}
		
		protected void OnResign(String Message)
		{
			if (Resign != null)
			{
				Resign(this, new ResignEventArgs(Message));
			}
		}
	}
}