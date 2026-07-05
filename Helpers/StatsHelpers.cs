namespace EventTrackerApp.Helpers;

public static class StatsHelpers
{
    // Source - https://stackoverflow.com/a/21009506
    // Posted by mike, modified by community. See post 'Timeline' for change history
    // Retrieved 2026-07-05, License - CC BY-SA 4.0

    /// <summary>
    /// Return the quartile values of an ordered set of doubles
    ///   assume the sorting has already been done.
    ///   
    /// This actually turns out to be a bit of a PITA, because there is no universal agreement 
    ///   on choosing the quartile values. In the case of odd values, some count the median value
    ///   in finding the 1st and 3rd quartile and some discard the median value. 
    ///   the two different methods result in two different answers.
    ///   The below method produces the arithmatic mean of the two methods, and insures the median
    ///   is given it's correct weight so that the median changes as smoothly as possible as 
    ///   more data ppints are added.
    ///    
    /// This method uses the following logic:
    /// 
    /// ===If there are an even number of data points:
    ///    Use the median to divide the ordered data set into two halves. 
    ///    The lower quartile value is the median of the lower half of the data. 
    ///    The upper quartile value is the median of the upper half of the data.
    ///    
    /// ===If there are (4n+1) data points:
    ///    The lower quartile is 25% of the nth data value plus 75% of the (n+1)th data value.
    ///    The upper quartile is 75% of the (3n+1)th data point plus 25% of the (3n+2)th data point.
    ///    
    ///===If there are (4n+3) data points:
    ///   The lower quartile is 75% of the (n+1)th data value plus 25% of the (n+2)th data value.
    ///   The upper quartile is 25% of the (3n+2)th data point plus 75% of the (3n+3)th data point.
    /// 
    /// </summary>
    public static Tuple<decimal, decimal, decimal> Quartiles(decimal[] values)
    {
        values.Sort();

        int size = values.Length;
        int mid = size / 2; //this is the mid from a zero based index, eg mid of 7 = 3;

        decimal quartile1 = 0;
        decimal quartile2 = 0;
        decimal quartile3 = 0;

        if (size % 2 == 0)
        {
            //================ EVEN NUMBER OF POINTS: =====================
            //even between low and high point
            quartile2 = (values[mid - 1] + values[mid]) / 2;

            int midMid = mid / 2;

            //easy split 
            if (mid % 2 == 0)
            {
                quartile1 = (values[midMid - 1] + values[midMid]) / 2;
                quartile3 = (values[mid + midMid - 1] + values[mid + midMid]) / 2;
            }
            else
            {
                quartile1 = values[midMid];
                quartile3 = values[midMid + mid];
            }
        }
        else if (size == 1)
        {
            //================= special case, sorry ================
            quartile1 = values[0];
            quartile2 = values[0];
            quartile3 = values[0];
        }
        else
        {
            //odd number so the median is just the midpoint in the array.
            quartile2 = values[mid];

            if ((size - 1) % 4 == 0)
            {
                //======================(4n-1) POINTS =========================
                int n = (size - 1) / 4;
                quartile1 = (values[n - 1] * .25m) + (values[n] * .75m);
                quartile3 = (values[3 * n] * .75m) + (values[3 * n + 1] * .25m);
            }
            else if ((size - 3) % 4 == 0)
            {
                //======================(4n-3) POINTS =========================
                int n = (size - 3) / 4;

                quartile1 = (values[n] * .75m) + (values[n + 1] * .25m);
                quartile3 = (values[3 * n + 1] * .25m) + (values[3 * n + 2] * .75m);
            }
        }

        return new(quartile1, quartile2, quartile3);
    }

}