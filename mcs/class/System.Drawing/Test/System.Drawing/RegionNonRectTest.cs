//
// System.Drawing.Region non-rectangular unit tests
//
// Authors:
//	Sebastien Pouliot  <sebastien@ximian.com>
//
// Copyright (C) 2006 Novell, Inc (http://www.novell.com)
//
// Permission is hereby granted, free of charge, to any person obtaining
// a copy of this software and associated documentation files (the
// "Software"), to deal in the Software without restriction, including
// without limitation the rights to use, copy, modify, merge, publish,
// distribute, sublicense, and/or sell copies of the Software, and to
// permit persons to whom the Software is furnished to do so, subject to
// the following conditions:
//
// The above copyright notice and this permission notice shall be
// included in all copies or substantial portions of the Software.
//
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND,
// EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF
// MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND
// NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE
// LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION
// OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION
// WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.
//

using System;
using System.Drawing.Imaging;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Security.Permissions;
using NUnit.Framework;

namespace MonoTests.System.Drawing {

	/* NOTE: General tests and rectangular region tests are located in TestRegion.cs */
	/*       Here we exclusively tests non-rectangular (GraphicsPath based) regions. */

	[TestFixture]
	public class RegionNonRectTest {

		private Bitmap bitmap;
		private Graphics graphic;
		private Matrix matrix;
		private GraphicsPath sp1, sp2, sp3, sp4;

		[TestFixtureSetUp]
		public void FixtureSetUp ()
		{
			bitmap = new Bitmap (10, 10);
			graphic = Graphics.FromImage (bitmap);
			matrix = new Matrix ();

			sp1 = new GraphicsPath ();
			sp1.AddPolygon (new Point[4] { new Point (0, 0), new Point (3, 0), new Point (3, 3), new Point (0, 3) });

			sp2 = new GraphicsPath ();
			sp2.AddPolygon (new Point[4] { new Point (2, 2), new Point (5, 2), new Point (5, 5), new Point (2, 5) });

			sp3 = new GraphicsPath ();
			sp3.AddPolygon (new Point[4] { new Point (6, 0), new Point (9, 0), new Point (9, 3), new Point (6, 3) });

			sp4 = new GraphicsPath ();
			sp4.AddPolygon (new Point[4] { new Point (8, 0), new Point (11, 0), new Point (11, 3), new Point (8, 3) });
		}

		// a region with an "empty ctor" graphic path is "empty" (i.e. not infinite)
		private void CheckEmpty (string prefix, Region region)
		{
			Assert.IsTrue (region.IsEmpty (graphic), prefix + "IsEmpty");
			Assert.IsFalse (region.IsInfinite (graphic), prefix + "graphic");

			RectangleF rect = region.GetBounds (graphic);
			Assert.AreEqual (0f, rect.X, prefix + "GetBounds.X");
			Assert.AreEqual (0f, rect.Y, prefix + "GetBounds.Y");
			Assert.AreEqual (0f, rect.Width, prefix + "GetBounds.Width");
			Assert.AreEqual (0f, rect.Height, prefix + "GetBounds.Height");
		}

		[Test]
		public void Region_Ctor_GraphicsPath_Empty ()
		{
			Region region = new Region (new GraphicsPath ());
			CheckEmpty ("GraphicsPath.", region);

			Region clone = region.Clone ();
			CheckEmpty ("Clone.", region);
		}

		[Test]
		[Ignore ("this does not work when using MS GDI+ - with or without Mono")]
		public void Region_Ctor_RegionData ()
		{
			Region region = new Region (new GraphicsPath ());
			RegionData data = region.GetRegionData ();
			Region r2 = new Region (data);
			CheckEmpty ("RegionData.", region);
		}

		[Test]
		public void Region_Ctor_GraphicsPath ()
		{
			GraphicsPath gp = new GraphicsPath ();
			Region region = new Region (gp);
			CheckEmpty ("GraphicsPath.", region);

			Region clone = region.Clone ();
			CheckEmpty ("Clone.", region);
		}

		private void CheckInfiniteBounds (GraphicsPath path)
		{
			RectangleF rect = path.GetBounds ();
			Assert.AreEqual (-4194304f, rect.X, "Bounds.X");
			Assert.AreEqual (-4194304f, rect.Y, "Bounds.Y");
			Assert.AreEqual (8388608f, rect.Width, "Bounds.Width");
			Assert.AreEqual (8388608f, rect.Height, "Bounds.Height");
		}

		[Test]
		public void Region_Curve_IsInfinite ()
		{
			Point[] points = new Point[2] { new Point (-4194304, -4194304), new Point (4194304, 4194304) };
			GraphicsPath gp = new GraphicsPath ();
			gp.AddCurve (points);
			CheckInfiniteBounds (gp);

			Region region = new Region (gp);
			Assert.IsFalse (region.IsInfinite (graphic), "IsInfinite");
			// note: infinity isn't based on the bounds
		}

		[Test]
		[Category ("NotWorking")]
		public void Region_Polygon4_IsInfinite ()
		{
			Point[] points = new Point[4] { new Point (-4194304, -4194304), new Point (-4194304, 4194304), new Point (4194304, 4194304), new Point (4194304, -4194304) };
			GraphicsPath gp = new GraphicsPath ();
			gp.AddPolygon (points);
			CheckInfiniteBounds (gp);

			Region region = new Region (gp);
			Assert.IsTrue (region.IsInfinite (graphic), "IsInfinite");
		}

		[Test]
		[Category ("NotWorking")]
		public void Region_Polygon5_IsInfinite ()
		{
			// overlap the first/last point
			Point[] points = new Point[5] { new Point (-4194304, -4194304), new Point (-4194304, 4194304), new Point (4194304, 4194304), new Point (4194304, -4194304), new Point (-4194304, -4194304) };
			GraphicsPath gp = new GraphicsPath ();
			gp.AddPolygon (points);
			CheckInfiniteBounds (gp);

			Region region = new Region (gp);
			Assert.IsTrue (region.IsInfinite (graphic), "IsInfinite");
		}

		[Test]
		[Category ("NotWorking")]
		public void Region_Rectangle_IsInfinite ()
		{
			GraphicsPath gp = new GraphicsPath ();
			gp.AddRectangle (new Rectangle (-4194304, -4194304, 8388608, 8388608));
			CheckInfiniteBounds (gp);

			Region region = new Region (gp);
			Assert.IsTrue (region.IsInfinite (graphic), "IsInfinite");
		}

		[Test]
		public void Curve_GetRegionScans ()
		{
			Point[] points = new Point[2] { new Point (-4194304, -4194304), new Point (4194304, 4194304) };
			GraphicsPath gp = new GraphicsPath ();
			gp.AddCurve (points);
			Region region = new Region (gp);
			// too big, returns 0
			Assert.AreEqual (0, region.GetRegionScans (matrix).Length, "GetRegionScans");
		}

		private void DisplaySmallRegion (Region region, int ox, int oy, int width, int height)
		{
			for (int y = oy ; y < height - 1; y++) {
				for (int x = ox; x < width - 1; x++) {
					if (region.IsVisible (x, y))
						Console.Write ("X");
					else
						Console.Write (".");
				}
				Console.WriteLine ();
			}
		}

		private void DisplaySmallRegion (Region region, int width, int height)
		{
			DisplaySmallRegion (region, -1, -1, width, height);
		}


		private void CompareSmallRegion (Region region, bool[] expected, int ox, int oy, int width, int height)
		{
			int p = 0;
			for (int y = oy; y < height + oy; y++) {
				for (int x = ox; x < width + ox; x++) {
					Assert.AreEqual (expected[p], region.IsVisible (x, y), String.Format ("{0},{1}", x, y));
					p++;
				}
			}
		}

		private void CompareSmallRegion (Region region, bool[] expected, int width, int height)
		{
			CompareSmallRegion (region, expected, -1, -1, width, height);
		}

		private void CheckRectF (string msg, int x, int y, int w, int h, RectangleF rect)
		{
			Assert.AreEqual (x, rect.X, msg + ".X");
			Assert.AreEqual (y, rect.Y, msg + ".Y");
			Assert.AreEqual (w, rect.Width, msg + ".Width");
			Assert.AreEqual (h, rect.Height, msg + ".Height");
		}

		static bool[] sunion = new bool[49] {
			false, false, false, false, false, false, false, // .......
			false, true,  true,  true,  false, false, false, // .XXX...
			false, true,  true,  true,  false, false, false, // .XXX...
			false, true,  true,  true,  true,  true,  false, // .XXXXX.
			false, false, false, true,  true,  true,  false, // ...XXX.
			false, false, false, true,  true,  true,  false, // ...XXX.
			false, false, false, false, false, false, false, // .......
		};

		[Test]
		public void SmallUnion1 ()
		{
			Region region = new Region (sp1);
			region.Union (sp2);
			CompareSmallRegion (region, sunion, 7, 7);

			RectangleF[] scans = region.GetRegionScans (matrix);
			Assert.AreEqual (3, scans.Length, "GetRegionScans");
			CheckRectF ("[0]", 0, 0, 3, 2, scans[0]);
			CheckRectF ("[1]", 0, 2, 5, 1, scans[1]);
			CheckRectF ("[2]", 2, 3, 3, 2, scans[2]);
		}

		[Test]
		public void SmallUnion2 ()
		{
			Region region = new Region (sp2);
			region.Union (sp1);
			CompareSmallRegion (region, sunion, 7, 7);

			RectangleF[] scans = region.GetRegionScans (matrix);
			Assert.AreEqual (3, scans.Length, "GetRegionScans");
			CheckRectF ("[0]", 0, 0, 3, 2, scans[0]);
			CheckRectF ("[1]", 0, 2, 5, 1, scans[1]);
			CheckRectF ("[2]", 2, 3, 3, 2, scans[2]);
		}

		static bool[] self1 = new bool[49] {
			false, false, false, false, false, false, false, // .......
			false, true,  true,  true,  false, false, false, // .XXX...
			false, true,  true,  true,  false, false, false, // .XXX...
			false, true,  true,  true,  false, false, false, // .XXX...
			false, false, false, false, false, false, false, // .......
			false, false, false, false, false, false, false, // .......
			false, false, false, false, false, false, false, // .......
		};

		[Test]
		public void SmallUnion_Self1 ()
		{
			Region region = new Region (sp1);
			region.Union (sp1);
			CompareSmallRegion (region, self1, 7, 7);

			RectangleF[] scans = region.GetRegionScans (matrix);
			Assert.AreEqual (1, scans.Length, "GetRegionScans");
			CheckRectF ("[0]", 0, 0, 3, 3, scans[0]);
		}

		static bool[] self2 = new bool[49] {
			false, false, false, false, false, false, false, // .......
			false, false, false, false, false, false, false, // .......
			false, false, false, false, false, false, false, // .......
			false, false, false, true,  true,  true,  false, // ...XXX.
			false, false, false, true,  true,  true,  false, // ...XXX.
			false, false, false, true,  true,  true,  false, // ...XXX.
			false, false, false, false, false, false, false, // .......
		};

		[Test]
		public void SmallUnion_Self2 ()
		{
			Region region = new Region (sp2);
			region.Union (sp2);
			CompareSmallRegion (region, self2, 7, 7);

			RectangleF[] scans = region.GetRegionScans (matrix);
			Assert.AreEqual (1, scans.Length, "GetRegionScans");
			CheckRectF ("[0]", 2, 2, 3, 3, scans[0]);
		}

		static bool[] sintersection = new bool[49] {
			false, false, false, false, false, false, false, // .......
			false, false, false, false, false, false, false, // .......
			false, false, false, false, false, false, false, // .......
			false, false, false, true,  false, false, false, // ...X...
			false, false, false, false, false, false, false, // .......
			false, false, false, false, false, false, false, // .......
			false, false, false, false, false, false, false, // .......
		};

		[Test]
		public void SmallIntersection1 ()
		{
			Region region = new Region (sp1);
			region.Intersect (sp2);
			CompareSmallRegion (region, sintersection, 7, 7);

			RectangleF[] scans = region.GetRegionScans (matrix);
			Assert.AreEqual (1, scans.Length, "GetRegionScans");
			CheckRectF ("[0]", 2, 2, 1, 1, scans[0]);
		}

		[Test]
		public void SmallIntersection2 ()
		{
			Region region = new Region (sp2);
			region.Intersect (sp1);
			CompareSmallRegion (region, sintersection, 7, 7);

			RectangleF[] scans = region.GetRegionScans (matrix);
			Assert.AreEqual (1, scans.Length, "GetRegionScans");
			CheckRectF ("[0]", 2, 2, 1, 1, scans[0]);
		}

		[Test]
		public void SmallIntersection_Self1 ()
		{
			Region region = new Region (sp1);
			region.Intersect (sp1);
			CompareSmallRegion (region, self1, 7, 7);

			RectangleF[] scans = region.GetRegionScans (matrix);
			Assert.AreEqual (1, scans.Length, "GetRegionScans");
			CheckRectF ("[0]", 0, 0, 3, 3, scans[0]);
		}

		[Test]
		public void SmallIntersection_Self2 ()
		{
			Region region = new Region (sp2);
			region.Intersect (sp2);
			CompareSmallRegion (region, self2, 7, 7);

			RectangleF[] scans = region.GetRegionScans (matrix);
			Assert.AreEqual (1, scans.Length, "GetRegionScans");
			CheckRectF ("[0]", 2, 2, 3, 3, scans[0]);
		}

		static bool[] sexclude1 = new bool[49] {
			false, false, false, false, false, false, false, // .......
			false, true,  true,  true,  false, false, false, // .XXX...
			false, true,  true,  true,  false, false, false, // .XXX...
			false, true,  true,  false, false, false, false, // .XX....
			false, false, false, false, false, false, false, // .......
			false, false, false, false, false, false, false, // .......
			false, false, false, false, false, false, false, // .......
		};

		[Test]
		public void SmallExclude1 ()
		{
			Region region = new Region (sp1);
			region.Exclude (sp2);
			CompareSmallRegion (region, sexclude1, 7, 7);

			RectangleF[] scans = region.GetRegionScans (matrix);
			Assert.AreEqual (2, scans.Length, "GetRegionScans");
			CheckRectF ("[0]", 0, 0, 3, 2, scans[0]);
			CheckRectF ("[1]", 0, 2, 2, 1, scans[1]);
		}

		static bool[] sexclude2 = new bool[49] {
			false, false, false, false, false, false, false, // .......
			false, false, false, false, false, false, false, // .......
			false, false, false, false, false, false, false, // .......
			false, false, false, false, true,  true,  false, // ....XX.
			false, false, false, true,  true,  true,  false, // ...XXX.
			false, false, false, true,  true,  true,  false, // ...XXX.
			false, false, false, false, false, false, false, // .......
		};

		[Test]
		public void SmallExclude2 ()
		{
			Region region = new Region (sp2);
			region.Exclude (sp1);
			CompareSmallRegion (region, sexclude2, 7, 7);

			RectangleF[] scans = region.GetRegionScans (matrix);
			Assert.AreEqual (2, scans.Length, "GetRegionScans");
			CheckRectF ("[0]", 3, 2, 2, 1, scans[0]);
			CheckRectF ("[1]", 2, 3, 3, 2, scans[1]);
		}

		static bool[] sempty = new bool[49] {
			false, false, false, false, false, false, false, // .......
			false, false, false, false, false, false, false, // .......
			false, false, false, false, false, false, false, // .......
			false, false, false, false, false, false, false, // .......
			false, false, false, false, false, false, false, // .......
			false, false, false, false, false, false, false, // .......
			false, false, false, false, false, false, false, // .......
		};

		[Test]
		public void SmallExclude_Self1 ()
		{
			Region region = new Region (sp1);
			region.Exclude (sp1);
			CompareSmallRegion (region, sempty, 7, 7);

			RectangleF[] scans = region.GetRegionScans (matrix);
			Assert.AreEqual (0, scans.Length, "GetRegionScans");
		}

		[Test]
		public void SmallExclude_Self2 ()
		{
			Region region = new Region (sp2);
			region.Exclude (sp2);
			CompareSmallRegion (region, sempty, 7, 7);

			RectangleF[] scans = region.GetRegionScans (matrix);
			Assert.AreEqual (0, scans.Length, "GetRegionScans");
		}

		[Test]
		public void SmallComplement1 ()
		{
			Region region = new Region (sp1);
			region.Complement (sp2);
			CompareSmallRegion (region, sexclude2, 7, 7);

			RectangleF[] scans = region.GetRegionScans (matrix);
			Assert.AreEqual (2, scans.Length, "GetRegionScans");
			CheckRectF ("[0]", 3, 2, 2, 1, scans[0]);
			CheckRectF ("[1]", 2, 3, 3, 2, scans[1]);
		}

		[Test]
		public void SmallComplement2 ()
		{
			Region region = new Region (sp2);
			region.Complement (sp1);
			CompareSmallRegion (region, sexclude1, 7, 7);

			RectangleF[] scans = region.GetRegionScans (matrix);
			Assert.AreEqual (2, scans.Length, "GetRegionScans");
			CheckRectF ("[0]", 0, 0, 3, 2, scans[0]);
			CheckRectF ("[1]", 0, 2, 2, 1, scans[1]);
		}

		[Test]
		public void SmallComplement_Self1 ()
		{
			Region region = new Region (sp1);
			region.Complement (sp1);
			CompareSmallRegion (region, sempty, 7, 7);

			RectangleF[] scans = region.GetRegionScans (matrix);
			Assert.AreEqual (0, scans.Length, "GetRegionScans");
		}

		[Test]
		public void SmallComplement_Self2 ()
		{
			Region region = new Region (sp2);
			region.Complement (sp2);
			CompareSmallRegion (region, sempty, 7, 7);

			RectangleF[] scans = region.GetRegionScans (matrix);
			Assert.AreEqual (0, scans.Length, "GetRegionScans");
		}

		static bool[] sxor = new bool[49] {
			false, false, false, false, false, false, false, // .......
			false, true,  true,  true,  false, false, false, // .XXX...
			false, true,  true,  true,  false, false, false, // .XXX...
			false, true,  true,  false, true,  true,  false, // .XX.XX.
			false, false, false, true,  true,  true,  false, // ...XXX.
			false, false, false, true,  true,  true,  false, // ...XXX.
			false, false, false, false, false, false, false, // .......
		};

		[Test]
		public void SmallXor1 ()
		{
			Region region = new Region (sp1);
			region.Xor (sp2);
			CompareSmallRegion (region, sxor, 7, 7);

			RectangleF[] scans = region.GetRegionScans (matrix);
			Assert.AreEqual (4, scans.Length, "GetRegionScans");
			CheckRectF ("[0]", 0, 0, 3, 2, scans[0]);
			CheckRectF ("[1]", 0, 2, 2, 1, scans[1]);
			CheckRectF ("[2]", 3, 2, 2, 1, scans[2]);
			CheckRectF ("[3]", 2, 3, 3, 2, scans[3]);
		}

		[Test]
		public void SmallXor2 ()
		{
			Region region = new Region (sp2);
			region.Xor (sp1);
			CompareSmallRegion (region, sxor, 7, 7);

			RectangleF[] scans = region.GetRegionScans (matrix);
			Assert.AreEqual (4, scans.Length, "GetRegionScans");
			CheckRectF ("[0]", 0, 0, 3, 2, scans[0]);
			CheckRectF ("[1]", 0, 2, 2, 1, scans[1]);
			CheckRectF ("[2]", 3, 2, 2, 1, scans[2]);
			CheckRectF ("[3]", 2, 3, 3, 2, scans[3]);
		}

		[Test]
		public void SmallXor_Self1 ()
		{
			Region region = new Region (sp1);
			region.Xor (sp1);
			CompareSmallRegion (region, sempty, 7, 7);

			RectangleF[] scans = region.GetRegionScans (matrix);
			Assert.AreEqual (0, scans.Length, "GetRegionScans");
		}

		[Test]
		public void SmallXor_Self2 ()
		{
			Region region = new Region (sp2);
			region.Xor (sp2);
			CompareSmallRegion (region, sempty, 7, 7);

			RectangleF[] scans = region.GetRegionScans (matrix);
			Assert.AreEqual (0, scans.Length, "GetRegionScans");
		}

		[Test]
		public void NegativeXor ()
		{
			GraphicsPath neg = new GraphicsPath ();
			// identical result (matrix) of XOR but we're using negative coordinates
			neg.AddPolygon (new Point[4] { new Point (-2, -2), new Point (1, -2), new Point (1, 1), new Point (-2, 1) });

			Region region = new Region (sp1);
			region.Xor (neg);
			CompareSmallRegion (region, sxor, -3, -3, 7, 7);
		}

		static bool[] ni_union = new bool[55] {
			false, false, false, false, false, false, false, false, false, false, false, // ...........
			false, true,  true,  true,  false, false, false, true,  true,  true,  false, // .XXX...XXX.
			false, true,  true,  true,  false, false, false, true,  true,  true,  false, // .XXX...XXX.
			false, true,  true,  true,  false, false, false, true,  true,  true,  false, // .XXX...XXX.
			false, false, false, false, false, false, false, false, false, false, false, // ...........
		};

		[Test]
		public void UnionWithoutIntersection ()
		{
			Region region = new Region (sp1);
			region.Union (sp3);
			CompareSmallRegion (region, ni_union, 11,5 );
		}

		[Test]
		// libgdiplus: both region are considered inside as intersecting rectangle because
		// part of them co-exists in the same 8x8 bitmap. Full algorithm apply but results
		// in an empty bitmap
		public void IntersectionWithoutIntersection ()
		{
			Region region = new Region (sp1);
			region.Intersect (sp3);
			CompareSmallRegion (region, sempty, 7, 7);
		}

		[Test]
		// libgdiplus: no intersection results in an empty bitmap (optimization)
		public void IntersectionWithoutIntersection_Large ()
		{
			Region region = new Region (sp1);
			region.Intersect (sp4);
			CompareSmallRegion (region, sempty, 7, 7);
		}

		[Test]
		// libgdiplus: both region are considered inside as intersecting rectangle because
		// part of them co-exists in the same 8x8 bitmap. Full algorithm apply but results
		// as a copy of sp1
		public void ExcludeWithoutIntersection ()
		{
			Region region = new Region (sp1);
			region.Exclude (sp3);
			CompareSmallRegion (region, self1, 7, 7);
		}

		[Test]
		// libgdiplus: no intersection results in a clone of sp1 (optimization)
		public void ExcludeWithoutIntersection_Large ()
		{
			Region region = new Region (sp1);
			region.Exclude (sp4);
			CompareSmallRegion (region, self1, 7, 7);
		}

		[Test]
		// libgdiplus: both region are considered inside as intersecting rectangle because
		// part of them co-exists in the same 8x8 bitmap. Full algorithm apply but results
		// as a copy of sp1
		public void ComplementWithoutIntersection ()
		{
			Region region = new Region (sp3);
			region.Complement (sp1);
			CompareSmallRegion (region, self1, 7, 7);
		}

		[Test]
		// libgdiplus: no intersection results in a clone of sp1 (optimization)
		public void ComplementWithoutIntersection_Large ()
		{
			Region region = new Region (sp4);
			region.Complement (sp1);
			CompareSmallRegion (region, self1, 7, 7);
		}

		[Test]
		// libgdiplus: both region are considered inside as intersecting rectangle because
		// part of them co-exists in the same 8x8 bitmap.
		public void XorWithoutIntersection ()
		{
			Region region = new Region (sp1);
			region.Xor (sp3);
			CompareSmallRegion (region, ni_union, 11, 5);
		}

		static bool[] ni_xor = new bool[65] {
			false, false, false, false, false, false, false, false, false, false, false, false, false, // .............
			false, true,  true,  true,  false, false, false, false, false, true,  true,  true,  false, // .XXX.....XXX.
			false, true,  true,  true,  false, false, false, false, false, true,  true,  true,  false, // .XXX.....XXX.
			false, true,  true,  true,  false, false, false, false, false, true,  true,  true,  false, // .XXX.....XXX.
			false, false, false, false, false, false, false, false, false, false, false, false, false, // .............
		};

		[Test]
		// libgdiplus: both region aren't considered as an intersection because they do 
		// not co-exists in the same 8x8 bitmap. In this case the xor function calls the
		// union code (optimization).
		public void XorWithoutIntersection_Large ()
		{
			Region region = new Region (sp1);
			region.Xor (sp4);
			CompareSmallRegion (region, ni_xor, 13, 5);
		}

		[Test]
		public void IsEqual ()
		{
			Region r1 = new Region (sp1);
			Region r2 = new Region (sp2);
			Region r3 = new Region (sp3);
			Region r4 = new Region (sp4);
			// with self
			Assert.IsTrue (r1.Equals (r1, graphic), "r1-r1");
			Assert.IsTrue (r2.Equals (r2, graphic), "r2-r2");
			Assert.IsTrue (r3.Equals (r3, graphic), "r3-r3");
			Assert.IsTrue (r4.Equals (r4, graphic), "r4-r4");
			// with a different
			Assert.IsFalse (r1.Equals (r4, graphic), "r1-r4");
			Assert.IsFalse (r2.Equals (r3, graphic), "r2-r3");
			Assert.IsFalse (r3.Equals (r2, graphic), "r3-r2");
			Assert.IsFalse (r4.Equals (r1, graphic), "r4-r1");
			// with same (not self)
			Region r5 = r1.Clone ();
			r1.Exclude (r4);
			Assert.IsTrue (r1.Equals (r5, graphic), "r1-r5");
			Assert.IsTrue (r5.Equals (r1, graphic), "r5-r1");
			Assert.IsFalse (r5.Equals (r4, graphic), "r5-r4");
			Assert.IsFalse (r4.Equals (r5, graphic), "r4-r5");
		}

		[Test]
		public void Translate_Int ()
		{
			Region r1 = new Region (sp1);
			Region r2 = new Region (sp2);
			r2.Translate (-2, -2);
			r1.Intersect (r2);
			CompareSmallRegion (r1, self1, 7, 7);
		}

		[Test]
		public void Translate_Float ()
		{
			Region r1 = new Region (sp1);
			Region r2 = new Region (sp2);
			r2.Translate (-2.0f, -2.0f);
			r1.Intersect (r2);
			CompareSmallRegion (r1, self1, 7, 7);
		}

		[Test]
		public void EmptyPathWithInfiniteRegion ()
		{
			GraphicsPath gp = new GraphicsPath ();
			Region region = new Region ();
			Assert.IsTrue (region.IsInfinite (graphic), "IsInfinite");

			region.Union (gp);
			Assert.IsTrue (region.IsInfinite (graphic), "Union-IsInfinite");

			region.Xor (gp);
			Assert.IsTrue (region.IsInfinite (graphic), "Xor-IsInfinite");

			region.Exclude (gp);
			Assert.IsTrue (region.IsInfinite (graphic), "Exclude-IsInfinite");

			region.Intersect (gp);
			Assert.IsTrue (region.IsEmpty (graphic), "Intersect-IsEmpty");

			region.MakeInfinite ();
			region.Complement (gp);
			Assert.IsTrue (region.IsEmpty (graphic), "Complement-IsEmpty");
		}

		[Test]
		public void EmptyRegionWithInfiniteRegion ()
		{
			Region empty = new Region ();
			empty.MakeEmpty ();
			Assert.IsTrue (empty.IsEmpty (graphic), "IsEmpty");

			Region region = new Region ();
			Assert.IsTrue (region.IsInfinite (graphic), "IsInfinite");

			region.Union (empty);
			Assert.IsTrue (region.IsInfinite (graphic), "Union-IsInfinite");

			region.Xor (empty);
			Assert.IsTrue (region.IsInfinite (graphic), "Xor-IsInfinite");

			region.Exclude (empty);
			Assert.IsTrue (region.IsInfinite (graphic), "Exclude-IsInfinite");

			region.Intersect (empty);
			Assert.IsTrue (region.IsEmpty (graphic), "Intersect-IsEmpty");

			region.MakeInfinite ();
			region.Complement (empty);
			Assert.IsTrue (region.IsEmpty (graphic), "Complement-IsEmpty");
		}
	}
}
