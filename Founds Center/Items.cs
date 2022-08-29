using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Founds_Center
{
    public enum UsageFCenter
    {
        error = -1,
        software = 0,
        electricity = 1,
        hardware = 2,
        furniture = 3,
        fuel = 4,
        maintenance = 5,
        business_deals = 6,
        construction = 7,
        utensils = 8,
        office = 9,
        food = 10,
        operation_costs = 11,
        taxes = 12,
        vehicles = 13,
        grants = 14,
        other = 15
    }

    public class Items
    {
        public string fcenter { get; set; }
        public int center { get; set; }
        public int sum { get; set; }
        public string text { get; set; }

        public Items()
        {

        }

        public Items(string fcenter, int center, int sum, string text)
        {
            this.fcenter = fcenter;
            this.center = center;
            this.sum = sum;
            this.text = text;
        }

        public void Clean()
        {
            fcenter = "";
            center = 0;
            sum = 0;
            text = "";
        }

        /// <summary>
        /// Checks if the grid item is empty
        /// </summary>
        /// <returns>true if the item is empty</returns>
        public bool IsEmpty()
        {
            return (String.IsNullOrEmpty(fcenter) && center == 0 && sum == 0 && String.IsNullOrEmpty(text));
        }

        //O(logn) - binary search
        /// <summary>
        /// Checks if the item center can be found in the possible centers for usage
        /// </summary>
        /// <param name="centerData">The list of centers to check from</param>
        /// <returns>true if found;</returns>
        public bool CurrectCenter(CenterData[] centerData)
        {
            int low = 0;
            int high = centerData.Length-1;
            int mid;
            
            while (low <= high)
            {
                mid = (low + high) / 2;

                if (centerData[mid].center == center)
                {
                    return true; //found
                }
                else if (center > centerData[mid].center)
                {
                    low = mid + 1;
                }
                else
                {
                    high = mid - 1;
                }
            }

            return false; //not found
        }

        /// <summary>
        /// Parses number for enum
        /// </summary>
        /// <param name="fc">Our founds center</param>
        /// <returns>Our enum</returns>
        public static UsageFCenter Find_FCenter(int fc)
        {
            UsageFCenter fcentert;

            try
            {
                fcentert = (UsageFCenter)Enum.Parse(typeof(UsageFCenter), fc.ToString());
            }catch (Exception)
            {
                fcentert = UsageFCenter.error;
            }

            return fcentert;
        }

        /// <summary>
        /// Checks if the found center is currect
        /// Ex: 202202 - hardware item in 2022
        /// </summary>
        /// <returns>true if currect</returns>
        public bool IsFcenterCurrect()
        {
            if (String.IsNullOrEmpty(fcenter) || CheckForLatter(fcenter))
                return false;

            short year = Convert.ToInt16(fcenter.Substring(0, 4));
            short coin = Convert.ToInt16(fcenter.Substring(4, 1));
            UsageFCenter fc = Find_FCenter(Convert.ToInt16(fcenter[5..]));

            //Coin 0 - us dollars
            //Coin 1 - new shekels
            //Coin 2 - other
            return (DateTime.Today.Year == year && (coin == 0 || coin == 1 || coin == 2) && fc != UsageFCenter.error);
        }
        
        /// <summary>
        /// Checks the string for latter
        /// </summary>
        /// <param name="s">The string you want to check</param>
        /// <returns>true if latter found in the string</returns>
        public static bool CheckForLatter(string s)
        {
            for (int i = 0; i < s.Length; i++)
            {
                if ((s[i] >= 'a' && s[i] <= 'z') || (s[i] >= 'A' && s[i] <= 'Z'))
                    return true;
            }

            return false;
        }

    }
}
