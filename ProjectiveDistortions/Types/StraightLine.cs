
namespace ProjectiveDistortions.Types
{
    public class StraightLine
    {
        public int Distance { get; set; }
        public double Angle { get; set; }
        public int Vote { get; set; }
        public StraightLine(int distance, double angle, int vote)
        {
            Distance = distance;
            Angle = angle;
            Vote = vote;
        }
    }
}
