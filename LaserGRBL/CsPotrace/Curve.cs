using System;

namespace CsPotrace
{
    /// <summary>
    /// Holds the information about der produced curves
    /// </summary>
    /// 
    public struct Curve
    {
        /// <summary>
        /// Bezier or Line
        /// </summary>
        public CurveKind Kind;

        /// <summary>
        /// Startpoint
        /// </summary>
        public DPoint A;

        /// <summary>
        /// ControlPoint
        /// </summary>
        public DPoint ControlPointA;

        /// <summary>
        /// ControlPoint
        /// </summary>
        public DPoint ControlPointB;

        /// <summary>
        /// Endpoint
        /// </summary>
        public DPoint B;

        /// <summary>
        /// Creates a curve
        /// </summary>
        /// <param name="Kind"></param>
        /// <param name="A">Startpoint</param>
        /// <param name="ControlPointA">Controlpoint</param>
        /// <param name="ControlPointB">Controlpoint</param>
        /// <param name="B">Endpoint</param>
        public Curve(CurveKind Kind, DPoint A, DPoint ControlPointA, DPoint ControlPointB, DPoint B)
        {
            this.Kind = Kind;
            this.A = A;
            this.B = B;
            this.ControlPointA = ControlPointA;
            this.ControlPointB = ControlPointB;
        }

        public double LinearLength
        {
            get
            {
                double dX = B.X - A.X;
                double dY = B.Y - A.Y;
                return Math.Sqrt(dX * dX + dY * dY);
            }
        }
    }
}