﻿using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;

namespace Anki.Tracks
{
	class Program
	{
		static void Main(string[] args)
		{
			var tb = new TrackBuilder();
			tb.Run();
		}
	}

	public class TrackBuilder
	{
		private readonly List<int[,]> all = new List<int[,]>();

		public void Run()
		{
			System.IO.File.WriteAllText(@"tracks.txt", "");
			System.IO.File.WriteAllText(@"tracks.html", "<html><body><table><tr>");

			//set your available pieces here
			var palette = new Palette(straight: 4 + 2, corner: 6 + 2);

			try
			{
				Recurse(new List<Cell> { new Cell(new Pos(0, 0), 1) }, palette, 1);
				Recurse(new List<Cell> { new Cell(new Pos(0, 0), 5) }, palette, 1);
			}
			finally
			{
				System.IO.File.AppendAllText(@"tracks.html", "</tr></table></body></html>");
			}
		}

		void Recurse(List<Cell> cells, Palette palette, int level)
		{
			if (palette.IsEmpty())
				return;

			var last = cells.Last();
			var nextpos = NextPos(last);

			var hitcell = HitCell(cells, nextpos);

			if (hitcell == null)
			{
				for (var mode = 1; mode <= 12; ++mode)
				{
					DoNext(cells, palette, level, mode, last, nextpos);
				}
			}
			else
			{
				var first = cells.First();

				if (first.Pos == nextpos && CanStack(last.Mode, first.Mode))
				{
					Good(cells);
				}
				else
				{
					//straight track over (or under) another straight track here
					if (hitcell.Value.Mode == 1 || hitcell.Value.Mode == 2)
					{
						DoNext(cells, palette, level, 3, last, nextpos);
						DoNext(cells, palette, level, 4, last, nextpos);
					}
					else if (hitcell.Value.Mode == 3 || hitcell.Value.Mode == 4)
					{
						DoNext(cells, palette, level, 1, last, nextpos);
						DoNext(cells, palette, level, 2, last, nextpos);
					}
				}
			}
		}

		private void DoNext(List<Cell> cells, Palette palette, int level, int mode, Cell last, Pos nextpos)
		{
			if (palette.Avail(mode))
			{
				if (CanStack(last.Mode, mode))
				{
					Recurse(cells.Union(new[] { new Cell(nextpos, mode) }).ToList(), palette.Without(mode), level + 1);
				}
			}
		}

		private static bool CanStack(int f, int to)
		{
			return (new[] { 1, 9, 12 }.Contains(to) && new[] { 1, 5, 7 }.Contains(f))
				   || (new[] { 2, 6, 8 }.Contains(to) && new[] { 2, 10, 11 }.Contains(f))
				   || (new[] { 3, 7, 11 }.Contains(to) && new[] { 3, 6, 9 }.Contains(f))
				   || (new[] { 4, 5, 10 }.Contains(to) && new[] { 4, 8, 12 }.Contains(f));
		}

		private void Good(List<Cell> cells)
		{
			//if (cells.Count == 14) //track length
			{
				var orig = GenerateMap(cells);

				//remove already found tracks
				if (!HasSame(orig))
				{
					all.Add(orig);

					var text = GenerateTxt(cells);
					System.IO.File.AppendAllText(@"tracks.txt", text);

					var html = GenerateHtml(cells);

					html = "<td>" + html + "</td>";

					if (all.Count != 0 && (all.Count - 1) % 16 == 0)
					{
						html = "</tr><tr>" + html;
					}

					System.IO.File.AppendAllText(@"tracks.html", html);

					Console.WriteLine(text);
				}
			}
		}

		private bool HasSame(int[,] orig)
		{
			var rotate = orig;
			if (HasFlip(rotate)) return true;

			rotate = Rotate(rotate);
			if (HasFlip(rotate)) return true;

			rotate = Rotate(rotate);
			if (HasFlip(rotate)) return true;

			rotate = Rotate(rotate);
			if (HasFlip(rotate)) return true;

			return false;
		}

		private int[,] Rotate(int[,] map)
		{
			var lenx = map.GetLength(0);
			var leny = map.GetLength(1);

			var ret = new int[leny, lenx];

			for (var x = 0; x < lenx; ++x)
			{
				for (var y = 0; y < leny; ++y)
				{
					ret[leny - y - 1, x] = map[x, y];
				}
			}

			return ret;
		}

		private bool HasFlip(int[,] map)
		{
			if (HasMap(map)) return true;

			var flip = FlipHor(map);

			if (HasMap(flip)) return true;

			flip = FlipVert(map);

			if (HasMap(flip)) return true;

			return false;
		}

		private void DumpMap(int[,] map)
		{
			var lenx = map.GetLength(0);
			var leny = map.GetLength(1);

			for (var y = 0; y < leny; ++y)
			{
				for (var x = 0; x < lenx; ++x)
				{
					Console.Write(map[x, y]);
				}

				Console.WriteLine();
			}

			Console.WriteLine();
		}

		private int[,] FlipVert(int[,] map)
		{
			var lenx = map.GetLength(0);
			var leny = map.GetLength(1);

			var ret = new int[lenx, leny];

			for (var x = 0; x < lenx; ++x)
			{
				for (var y = 0; y < leny; ++y)
				{
					ret[x, leny - y - 1] = map[x, y];
				}
			}

			return ret;
		}

		private int[,] FlipHor(int[,] map)
		{
			var lenx = map.GetLength(0);
			var leny = map.GetLength(1);

			var ret = new int[lenx, leny];

			for (var x = 0; x < lenx; ++x)
			{
				for (var y = 0; y < leny; ++y)
				{
					ret[lenx - x - 1, y] = map[x, y];
				}
			}

			return ret;
		}

		private bool HasMap(int[,] map)
		{
			return all.Any(old => MapEquals(old, map));
		}

		private bool MapEquals(int[,] a1, int[,] a2)
		{
			var lenx1 = a1.GetLength(0);
			var leny1 = a1.GetLength(1);

			var lenx2 = a2.GetLength(0);
			var leny2 = a2.GetLength(1);

			if (lenx1 != lenx2 || leny1 != leny2) return false;

			for (var x = 0; x < lenx1; ++x)
			{
				for (var y = 0; y < leny1; ++y)
				{
					if (a1[x, y] != a2[x, y])
						return false;
				}
			}

			return true;
		}

		private string GenerateHtml(List<Cell> cells)
		{
			var text = new StringBuilder();

			var minx = cells.Min(a => a.Pos.X);
			var maxx = cells.Max(a => a.Pos.X);
			var miny = cells.Min(a => a.Pos.Y);
			var maxy = cells.Max(a => a.Pos.Y);

			for (var y = miny; y <= maxy; ++y)
			{
				for (var x = minx; x <= maxx; ++x)
				{
					var pos = new Pos(x, y);
					var symbol = 0;

					if (cells.Any(a => a.Pos == pos))
					{
						var cell = cells.First(a => a.Pos == pos);
						symbol = cell.Mode;
					}

					text.Append($"<img src=\"{symbol}.jpg\" />");
				}

				text.Append("<br />");
			}

			text.Append("<br />");

			return text.ToString();
		}

		private int[,] GenerateMap(List<Cell> cells)
		{
			var minx = cells.Min(a => a.Pos.X);
			var maxx = cells.Max(a => a.Pos.X);
			var miny = cells.Min(a => a.Pos.Y);
			var maxy = cells.Max(a => a.Pos.Y);

			var ret = new int[(maxx - minx + 1) * 3, (maxy - miny + 1) * 3];

			for (var y = miny; y <= maxy; ++y)
			{
				for (var x = minx; x <= maxx; ++x)
				{
					var pos = new Pos(x, y);

					foreach (var hit in cells.Where(cell => cell.Pos == pos))
					{
						var symbol = GetPircePattern(hit.Mode);

						for (var ya = 0; ya <= 2; ++ya)
						{
							for (var xa = 0; xa <= 2; ++xa)
							{
								if (symbol[ya, xa] == 1)
									ret[(x - minx) * 3 + xa, (y - miny) * 3 + ya] = 1;
							}
						}
					}
				}
			}

			return ret;
		}

		private static int[,] GetPircePattern(int mode)
		{
			switch (mode)
			{
				case 1:
				case 2:
					return new[,]
					{
						{0, 1, 0},
						{0, 1, 0},
						{0, 1, 0}
					};
				case 3:
				case 4:
					return new[,]
					{
						{0, 0, 0},
						{1, 1, 1},
						{0, 0, 0}
					};
				case 5:
				case 6:
					return new[,]
					{
						{0, 0, 0},
						{0, 1, 1},
						{0, 1, 0}
					};
				case 7:
				case 8:
					return new[,]
					{
						{0, 0, 0},
						{1, 1, 0},
						{0, 1, 0}
					};
				case 9:
				case 10:
					return new[,]
					{
						{0, 1, 0},
						{0, 1, 1},
						{0, 0, 0}
					};
				case 11:
				case 12:
					return new[,]
					{
						{0, 1, 0},
						{1, 1, 0},
						{0, 0, 0}
					};
				default:
					return new[,]
					{
						{0, 0, 0},
						{0, 0, 0},
						{0, 0, 0}
					};
			}
		}

		private string GenerateTxt(List<Cell> cells)
		{
			var text = new StringBuilder();

			text.AppendLine(all.Count + ": " + String.Join("-", cells.Select(a => a.Mode)));

			var minx = cells.Min(a => a.Pos.X);
			var maxx = cells.Max(a => a.Pos.X);
			var miny = cells.Min(a => a.Pos.Y);
			var maxy = cells.Max(a => a.Pos.Y);

			for (var y = miny; y <= maxy; ++y)
			{
				for (var x = minx; x <= maxx; ++x)
				{
					var pos = new Pos(x, y);
					var symbol = ' ';

					if (cells.Any(a => a.Pos == pos))
					{
						var cell = cells.First(a => a.Pos == pos);

						symbol = Symbol(cell.Mode);
					}

					text.Append(symbol);
				}

				text.AppendLine();
			}

			text.AppendLine();

			return text.ToString();
		}

		private static char Symbol(int mode)
		{
			switch (mode)
			{
				case 1:
				case 2:
					return '│';
				case 3:
				case 4:
					return '─';
				case 5:
				case 6:
					return '┌';
				case 11:
				case 12:
					return '┘';
				case 7:
				case 8:
					return '┐';
				case 9:
				case 10:
					return '└';
				default:
					return '*';
			}
		}

		private Cell? HitCell(List<Cell> cells, Pos nextpos)
		{
			if (cells.All(cell => cell.Pos != nextpos))
				return null;

			return cells.First(cell => cell.Pos == nextpos);
		}

		private Pos NextPos(Cell last)
		{
			switch (last.Mode)
			{
				case 02: return last.Pos.Up();
				case 10: return last.Pos.Up();
				case 11: return last.Pos.Up();
				case 01: return last.Pos.Down();
				case 05: return last.Pos.Down();
				case 07: return last.Pos.Down();
				case 04: return last.Pos.Left();
				case 08: return last.Pos.Left();
				case 12: return last.Pos.Left();
				case 03: return last.Pos.Right();
				case 09: return last.Pos.Right();
				case 06: return last.Pos.Right();
			}

			throw new Exception();
		}
	}

	public struct Palette
	{
		readonly int straight;
		readonly int corner;

		public Palette(int straight, int corner)
		{
			this.straight = straight;
			this.corner = corner;
		}

		public bool Avail(int mode)
		{
			if (mode >= 1 && mode <= 4)
			{
				return straight > 0;
			}
			else if (mode >= 5 && mode <= 12)
			{
				return corner > 0;
			}

			throw new Exception();
		}

		public Palette Without(int mode)
		{
			var nexts = straight;
			var nextc = corner;

			if (mode >= 1 && mode <= 4) nexts--;
			if (mode >= 5 && mode <= 12) nextc--;

			return new Palette(nexts, nextc);
		}

		public bool IsEmpty()
		{
			return straight == 0 && corner == 0;
		}
	}

	[DebuggerDisplay("{X}, {Y}")]
	public struct Pos
	{
		public int X { get; }
		public int Y { get; }

		public Pos(int x, int y)
		{
			X = x;
			Y = y;
		}

		public Pos Down()
		{
			return new Pos(X, Y + 1);
		}

		public Pos Up()
		{
			return new Pos(X, Y - 1);
		}

		public Pos Left()
		{
			return new Pos(X - 1, Y);
		}

		public Pos Right()
		{
			return new Pos(X + 1, Y);
		}

		public static bool operator ==(Pos a, Pos b)
		{
			return a.X == b.X && a.Y == b.Y;
		}

		public static bool operator !=(Pos a, Pos b)
		{
			return a.X != b.X || a.Y != b.Y;
		}

		public bool Equals(Pos other)
		{
			return X == other.X && Y == other.Y;
		}

		public override bool Equals(object obj)
		{
			if (ReferenceEquals(null, obj)) return false;
			return obj is Pos && Equals((Pos)obj);
		}

		public override int GetHashCode()
		{
			unchecked
			{
				return (X * 397) ^ Y;
			}
		}
	}

	[DebuggerDisplay("{Pos.X}, {Pos.Y}, {Mode}")]
	public struct Cell
	{
		public Pos Pos { get; }
		public int Mode { get; }

		public Cell(Pos pos, int mode)
		{
			Pos = pos;
			Mode = mode;
		}
	}
}
