namespace OW360Camera
{
    public class Utils
    {
        public static int NearestPowerOfTwo(int value)
        {
            int upper = 1, lower = 1;
            while (upper < value) {
                lower = upper;
                upper <<= 1;
            }

            if ((upper - value) <= (value - lower)) {
                return upper;
            }
            return lower;
        }
    }
}