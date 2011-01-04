// 
//  CommonShogiNotationHelpers.cs
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
	public class CommonShogiNotationHelpers
	{
		private static readonly Char[] VerticalNamings = { 'a', 'b', 'c', 'd', 'e', 'f', 'g', 'h', 'i' };
		private static readonly Char[] HorizontalNamings = { '1', '2', '3', '4', '5', '6', '7', '8', '9' };
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
			'K'  //OUSHOU    King
		};
		
		public static Char[] GetVerticalNamings()
		{
			return (Char[])VerticalNamings.Clone();
		}
		
		public static Char[] GetHorizontalNamings()
		{
			return (Char[])HorizontalNamings.Clone();
		}
		
		public static Char[] GetPieceNamings()
		{
			return (Char[])PieceNamings.Clone();
		}

		public static PieceType GetPieceByName(Char Name)
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

		public static int GetColByName(Char Name)
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

		public static int GetRowByName(Char Name)
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

	}
}

