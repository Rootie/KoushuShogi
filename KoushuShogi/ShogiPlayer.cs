// 
//  ShogiPlayer.cs
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
	public class Player
	{
		public String Name;
		public String Rank;
	}
	
	public interface IPlayerEngine : IDisposable
	{
		void StartGame(bool Blackplayer, FieldInfo[,] Board, int[,] OnHandPieces);
		void EndGame();
		void OponentMove(Move move);
		
		event EventHandler<MoveReadyEventArgs> MoveReady;
		event EventHandler<ResignEventArgs> Resign;
	}
	
	[Serializable]
	public sealed class MoveReadyEventArgs : EventArgs
	{
		public Move move;
		public MoveReadyEventArgs(Move move)
		{
			this.move = move;
		}
	}
	[Serializable]
	public sealed class ResignEventArgs : EventArgs
	{
		public String Message;
		public ResignEventArgs(String Message)
		{
			this.Message = Message;
		}
	}
	
	public class LocalPlayer : IPlayerEngine
	{
		#region Player implementation
		public event EventHandler<MoveReadyEventArgs> MoveReady;
		public event EventHandler<ResignEventArgs> Resign;

		public void StartGame (bool Blackplayer, FieldInfo[,] Board, int[,] OnHandPieces)
		{
			if (Blackplayer)
			{
				OnNeedMove(new EventArgs());
			}
		}

		public void EndGame()
		{
		}
		
		public void OponentMove (Move move)
		{
			OnNeedMove(new EventArgs());
		}

		public string Name {
			get;
			set;
		}
		#endregion

		#region IDisposable implementation
		public void Dispose ()
		{
		}
		#endregion		
		
		public void MakeMove(Move move)
		{
			if (MoveReady != null)
			{
				MoveReady(this, new MoveReadyEventArgs(move));
			}
		}
		
		protected void OnNeedMove(EventArgs e)
		{
			if (NeedMove != null)
			{
				NeedMove(this, e);
			}
		}
		
		public event EventHandler NeedMove;
	}
}
