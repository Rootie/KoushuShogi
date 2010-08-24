// 
//  PSN.cs
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
using System.IO;
using System.Text;

namespace Shogiban.FileFormat
{
	public class PSN
	{
		public static void Save(Game game, Stream stream)
		{
			StringBuilder data = new StringBuilder();
			data.AppendLine("[Sente \"" + game.BlackPlayer.Name + "\"]");
			data.AppendLine("[Gote \"" + game.WhitePlayer.Name + "\"]");
			
			data.AppendLine();
			
			int MoveNr = 1;
			foreach (Move move in game.Moves)
			{
				data.Append(MoveNr + "." + move.ToString() + " ");
				MoveNr++;
			}
			
			byte[] bytes = System.Text.Encoding.ASCII.GetBytes(data.ToString());
			stream.Write(bytes, 0, bytes.Length);
		}
	}
}

