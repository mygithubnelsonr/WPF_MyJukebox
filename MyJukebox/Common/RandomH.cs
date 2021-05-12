using System;
using System.Diagnostics;

namespace MyJukeboxWMPDapper.Common
{
    public class RandomH
    {
        #region Fields
        private string _numbersAsString;
        private int _nextNumber;
        private int[] numberArray;
        private static int numberCounter;
        #endregion

        #region Properties
        public string GetNumbersAsString
        {
            get { return _numbersAsString; }
        }

        public int GetFirstNumber
        {
            get
            {
                numberCounter++;
                return numberArray[0];
            }
        }

        public int GetNextNumber
        {
            get
            {
                var numbers = $"{ String.Join(Environment.NewLine, numberArray)}";
                Debug.Print($"numberArray numbers: { String.Join(",", numberArray)}");

                if (numberArray.Length == numberCounter)
                    numberCounter = 0;

                _nextNumber = numberArray[numberCounter];
                numberCounter++;
                return _nextNumber;
            }
        }

        public int GetPreviousNumber
        {
            get
            {
                _nextNumber = numberArray[numberCounter];
                numberCounter--;
                return _nextNumber;
            }
        }

        public int[] GetNumbersAsIntArray
        {
            get { return numberArray; }
        }
        #endregion

        #region CTOR
        public RandomH()
        {
            numberArray = null;
        }
        #endregion

        #region Methods
        public void InitRandomNumbers(int max, int min = 0)
        {
            _nextNumber = 0;

            numberCounter = 0;
            int LowerBound = min;
            int UpperBound = (max >= 1) ? max : 1;
            bool firsttime = true;
            int starti = 0;
            numberArray = new int[UpperBound];
            Random randomGenerator = new Random(DateTime.Now.Millisecond);
            do
            {
                int nogenerated = randomGenerator.Next(LowerBound, UpperBound + 1);
                // Note: randomGenerator.Next generates no. to UpperBound - 1 hence +1 
                // .... i got stuck at this pt & had to use the debugger. 
                if (firsttime) // if (firsttime == true) 
                {
                    numberArray[starti] = nogenerated; // we simply store the nogenerated in vararray 
                    firsttime = false;
                    starti++;
                }
                else // if (firsttime == false) 
                {
                    bool duplicate_flag = CheckDuplicate(nogenerated, starti, numberArray); // call to check in array 
                    if (!duplicate_flag) // duplicate_flag == false 
                    {
                        numberArray[starti] = nogenerated;
                        starti++;
                    }
                }
            }
            while (starti < UpperBound);
            //Debug.Print($"numberArrayCount={numberArray.Length}");
        }

        public bool CheckDuplicate(int newrandomNum, int loopcount, int[] function_array)
        {
            bool temp_duplicate = false;
            for (int j = 0; j < loopcount; j++)
            {
                if (function_array[j] == newrandomNum)
                {
                    temp_duplicate = true;
                    break;
                }
            }
            return temp_duplicate;
        }

        public void PrintArray(Array arr)
        {
            Console.Write("{");
            int count = 0;
            int li = arr.Length;
            foreach (object o in arr)
            {
                Console.Write("{0}", o);
                _numbersAsString += o.ToString();

                count++;
                //Condition to check whether ',' should be added in printing arrray 
                if (count < li)
                {
                    Console.Write(",");
                    _numbersAsString += ",";
                }
            }
            Console.WriteLine("}");
        }
        #endregion
    }
}
