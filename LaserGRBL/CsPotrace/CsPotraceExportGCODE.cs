//Copyright (c) 2016-2021 Diego Settimi - https://github.com/arkypita/

// This program is free software; you can redistribute it and/or modify  it under the terms of the GPLv3 General Public License as published by  the Free Software Foundation; either version 3 of the License, or (at  your option) any later version.
// This program is distributed in the hope that it will be useful, but  WITHOUT ANY WARRANTY; without even the implied warranty of  MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the GPLv3  General Public License for more details.
// You should have received a copy of the GPLv3 General Public License  along with this program; if not, write to the Free Software  Foundation, Inc., 59 Temple Place, Suite 330, Boston, MA 02111-1307,  USA. using System;

using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Globalization;
using System.Linq;
using CsPotrace.BezierToBiarc;

namespace CsPotrace
{
    /// <summary>
    /// Description of CsPotraceExportGCODE.
    /// </summary>
    public partial class Potrace
    {
        /// <summary>
        /// Exports a figure, created by Potrace from a Bitmap to a svg-formatted string
        /// </summary>
        /// <param name="list">Arraylist, which contains vectorinformations about the Curves</param>
        /// <param name="oX">Width of the exported cvg-File</param>
        /// <param name="oY">Height of the exported cvg-File</param>
        /// <returns></returns>
        public static List<string> Export2GCode(List<List<Curve>> list, float oX, float oY, double scale, string laserOnCode,
            string laserOffCode, Size originalImageSize, string skipcmd)
        {
            bool debug = false;

            Bitmap bmp = null;
            Graphics g = null;

            if (debug)
            {
                bmp = new Bitmap(originalImageSize.Width, originalImageSize.Height);
                g = Graphics.FromImage(bmp);
                g.SmoothingMode = SmoothingMode.HighQuality;
                g.Clear(Color.White);
            }

            var rv = list.SelectMany(curves => GetPathGC(curves, laserOnCode, laserOffCode, oX * scale, oY * scale, scale, g, skipcmd))
                .ToList();

            if (debug)
            {
                bmp.RotateFlip(RotateFlipType.RotateNoneFlipY);
                bmp.Save("preview.png");
                g.Dispose();
                bmp.Dispose();
            }

            return rv;
        }

        private static List<string> GetPathGC(List<Curve> Curves, string lOn, string lOff, double oX, double oY,
            double scale, Graphics g, string skipcmd)
        {
            List<string> rv = new List<string>();

            OnPathBegin(Curves, lOff, oX, oY, scale, rv, skipcmd);

            for (var index = 0; index < Curves.Count; index++)
            {
                Curve curve = Curves[index];
                OnPathSegment(curve, lOn, oX, oY, scale, rv, g, index);
            }

            OnPathEnd(Curves, lOff, oX, oY, scale, rv);

            return rv;
        }

        private static void OnPathSegment(Curve Curve, string lOn, double oX, double oY, double scale, List<string> rv,
            Graphics g, int idx)
        {
            string onCode = idx == 0 ? $" {lOn}" : "";

            if (double.IsNaN(Curve.LinearLength)) // problem?
            {
                return;
            }

            if (Curve.Kind == CurveKind.Line)
            {
                //trace line
                g?.DrawLine(Pens.DarkGray, (float) Curve.A.X, (float) Curve.A.Y, (float) Curve.B.X,
                    (float) Curve.B.Y);

                rv.Add($"G1 X{FormatNumber(Curve.B.X + oX, scale)} Y{FormatNumber(Curve.B.Y + oY, scale)}{onCode}");
            }

            if (Curve.Kind == CurveKind.Bezier)
            {
                CubicBezier cb = new CubicBezier(new Vector2((float) Curve.A.X, (float) Curve.A.Y),
                    new Vector2((float) Curve.ControlPointA.X, (float) Curve.ControlPointA.Y),
                    new Vector2((float) Curve.ControlPointB.X, (float) Curve.ControlPointB.Y),
                    new Vector2((float) Curve.B.X, (float) Curve.B.Y));

                g?.DrawBezier(Pens.Green,
                    AsPointF(cb.P1),
                    AsPointF(cb.C1),
                    AsPointF(cb.C2),
                    AsPointF(cb.P2));

                try
                {
                    List<BiArc> bal = Algorithm.ApproxCubicBezier(cb, 5, 2);
                    if (bal != null) //può ritornare null se ha troppi punti da processare
                    {
                        foreach (BiArc ba in bal)
                        {
                            if (!double.IsNaN(ba.A1.Length) && !double.IsNaN(ba.A1.LinearLength))
                            {
                                rv.Add($"{GetArcGC(ba.A1, oX, oY, scale, g)}{onCode}");
                            }

                            if (!double.IsNaN(ba.A2.Length) && !double.IsNaN(ba.A2.LinearLength))
                            {
                                rv.Add($"{GetArcGC(ba.A2, oX, oY, scale, g)}{onCode}");
                            }
                        }
                    }
                    else //same as exception
                    {
                        g?.DrawLine(Pens.DarkGray, (float) Curve.A.X, (float) Curve.A.Y, (float) Curve.B.X,
                            (float) Curve.B.Y);

                        rv.Add(
                            $"G1 X{FormatNumber(Curve.B.X + oX, scale)} Y{FormatNumber(Curve.B.Y + oY, scale)}{onCode}");
                    }
                }
                catch
                {
                    g?.DrawLine(Pens.DarkGray, (float) Curve.A.X, (float) Curve.A.Y, (float) Curve.B.X,
                        (float) Curve.B.Y);

                    rv.Add($"G1 X{FormatNumber(Curve.B.X + oX, scale)} Y{FormatNumber(Curve.B.Y + oY, scale)}{onCode}");
                }
            }
        }

        private static void OnPathBegin(List<Curve> Curves, string lOff, double oX, double oY, double scale,
            List<string> rv, string skipcmd)
        {
            if (Curves.Count > 0)
            {
                //fast go to position
                rv.Add(
                    $"{skipcmd} X{FormatNumber(Curves[0].A.X + oX, scale)} Y{FormatNumber(Curves[0].A.Y + oY, scale)} {lOff}");
                //turn on laser
                //rv.Add(lOn);
            }
        }

        private static void OnPathEnd(List<Curve> curves, string lOff, double oX, double oY, double scale,
            List<string> rv)
        {
            //turn off laser
            if (curves.Count > 0)
            {
                //rv.Add(lOff);
            }
        }

        private static string GetArcGC(Arc arc, double oX, double oY, double scale, Graphics g)
        {
            //http://www.cnccookbook.com/CCCNCGCodeArcsG02G03.htm
            //https://www.tormach.com/g02_g03.html

            if (arc.LinearLength > 2) //if not a small arc
            {
                g?.DrawArc(Pens.Red, arc.C.X - arc.r, arc.C.Y - arc.r, 2 * arc.r, 2 * arc.r,
                    arc.startAngle * 180.0f / (float) Math.PI, arc.sweepAngle * 180.0f / (float) Math.PI);

                return
                    $"G{(!arc.IsClockwise ? 2 : 3)} X{FormatNumber(arc.P2.X + oX, scale)} Y{FormatNumber(arc.P2.Y + oY, scale)} I{FormatNumber(arc.C.X - arc.P1.X, scale)} J{FormatNumber(arc.C.Y - arc.P1.Y, scale)}";
            }

            g?.DrawLine(Pens.DarkGray, arc.P1.X, arc.P1.Y, arc.P2.X, arc.P2.Y);

            return $"G1 X{FormatNumber(arc.P2.X + oX, scale)} Y{FormatNumber(arc.P2.Y + oY, scale)}";
        }

        private static string FormatNumber(double number, double scale)
        {
            double num = number / scale;
            return !double.IsNaN(num) ? num.ToString("0.###", CultureInfo.InvariantCulture) : "0";
        }

        public static PointF AsPointF(Vector2 v)
        {
            return new PointF(v.X, v.Y);
        }
    }
}