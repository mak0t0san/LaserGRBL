namespace CsPotrace
{
    /// <summary>
    /// Holds the coordinates of a Point
    /// </summary>
    public class DPoint
    {
        /// <summary>
        /// x-coordinate
        /// </summary>
        public double X;

        /// <summary>
        /// y-coordinate
        /// </summary>
        public double Y;

        /// <summary>
        /// Creates a point
        /// </summary>
        /// <param name="x">x-coordinate</param>
        /// <param name="y">y-coordinate</param>
        public DPoint(double x, double y)
        {
            X = x;
            Y = y;
        }

        public DPoint copy()
        {
            return new DPoint(X, Y);
        }

        public DPoint()
        {
        }
    }
}