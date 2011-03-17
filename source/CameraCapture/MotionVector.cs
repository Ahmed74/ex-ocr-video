using System;
using System.Collections.Generic;
using System.Text;

namespace CameraCapture
{
    public enum Direction { None, Left, Right, Top, Bottom };
    public class MotionVector
    {
        Direction direction;

        public MotionVector()
        {
            direction = Direction.None;
            magnitude = 0;
        }
        public Direction Direction
        {
            get { return direction; }
            set { direction = value; }
        }
        int magnitude;

        public int Magnitude
        {
            get { return magnitude; }
            set { magnitude = value; }
        }
    }
}
