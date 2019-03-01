using System;
using System.Collections.Generic;

namespace AircraftSimulator
{
    public delegate void ReportFlightDetails(bool overspeed = false); 

    static class Aircraft
    {
        public static bool ReachedMaxSpeed { get; private set; } = false; 
        public static bool Landed { get; private set; } = false;          

        private static void CheckIfLanded()
        {
            if (Speed == 0 && Altitude == 0 && ReachedMaxSpeed)
            {
                Landed = true;
            }
        }

        public static string MessageToPilot { get; set; } = "To start the flight, increase the speed and altitude.";

        public static event ReportFlightDetails reportFlightDetails;

        private static int speed = 0;
        public static int Speed
        {
            get { return speed; }
            set
            {
                if (value >= 1000)
                {
                    speed = 1000;
                    ReachedMaxSpeed = true;
                    MessageToPilot = "The maximum speed was fixed. You can begin to decline and landing.";
                }
                else
                {
                    speed = (value < 0) ? 0 : value;
                }

                if (value > 1000)
                {
                    reportFlightDetails(true); 
                    MessageToPilot = "The maximum speed of the aircraft is 1000 km / h! You can begin to decline and landing.";
                }
                else
                {
                    reportFlightDetails(false); 
                }

                CheckIfLanded();
            }
        }

        private static int altitude = 0;
        public static int Altitude
        {
            get { return altitude; }
            set
            {
                if (speed > 0)
                {
                    altitude = (value < 0) ? 0 : value;
                }
                else
                {
                    MessageToPilot = "Unable to change the height at zero speed.";
                }

                reportFlightDetails(); 

                CheckIfLanded();
            }
        }

        public static List<Dispatcher> dispatchers = new List<Dispatcher>(2); 

        public static void AddDispatcher(string name)
        {
            Dispatcher dispatcher = new Dispatcher(name);
            dispatchers.Add(dispatcher);
            reportFlightDetails += dispatcher.CheckFlight;  
        }

        public static void RemoveDispatcher(int index)
        {
            if (dispatchers.Count <= 2)
            {
                Aircraft.MessageToPilot = "The aircraft must be controlled by a minimum of 2 controllers!";
                return;
            }

            if (index < 0 || index >= dispatchers.Count)
            {
                Aircraft.MessageToPilot = "Invalid dispatcher number.";
                return;
            }

            PointsFromRemovedDispatchers += dispatchers[index].Points;  
            reportFlightDetails -= dispatchers[index].CheckFlight;  
            dispatchers.RemoveAt(index);  
        }

        public static int PointsFromRemovedDispatchers { get; private set; } = 0;
    }

    class Dispatcher
    {
        public string Name { get; }
        public int N { get; }  
        public int RecommendedAltitude { get; private set; }
        public string MessageToPilot { get; private set; }
        static Random rnd = new Random();

        private int points; 
        public int Points
        {
            get { return points; }
            private set
            {
                points = value;
                if (points >= 1000)
                {
                    MessageToPilot = "Airworthy";
                    throw new Exception($"The pilot is not suitable for flying, because scored 1000 penalty points from the dispatcher {Name}.");
                }
            }
        }

      
        private void CalculateRecommendedAltitude()
        {
            RecommendedAltitude = 7 * Aircraft.Speed - N;
            if (RecommendedAltitude < 0)
            {
                RecommendedAltitude = 0;
            }
        }

        public Dispatcher(string name)
        {
            Name = name;
            Points = 0;
            N = rnd.Next(-200, 201);  
            CalculateRecommendedAltitude();
        }

        public void CheckFlight(bool overspeed = false)
        {
            int difference = Math.Abs(Aircraft.Altitude - RecommendedAltitude);
            if (difference > 1000)
            {
                MessageToPilot = "The plane crashed";
                throw new Exception($"The plane crashed, because the pilot ignored the instructions of the dispatcher {Name}.");
            }
            else if (difference >= 600)
            {
                Points += 50;
                MessageToPilot = "Fine 50 points";
            }
            else if (difference >= 300)
            {
                Points += 25;
                MessageToPilot = "Fine 25 points";
            }
            else
            {
                MessageToPilot = "Normal flight";
            }

            if (overspeed)
            {
                Points += 100;
                MessageToPilot = "Slow down!";
            }

            if (Aircraft.Speed == 0 && Aircraft.Altitude > 0)
            {
                MessageToPilot = "The plane crashed";
                throw new Exception("The plane crashed, because reset speed to 0.");
            }

            CalculateRecommendedAltitude();
        }
    }


    static class ConsoleUserInterface
    {

        public static void Start()
        {
            Console.Title = "Flight simulator pilot";
            Console.SetWindowSize(105, 25);
            Console.SetBufferSize(105, 25);
            Console.WriteLine("\nWelcome to the flight simulator pilot.\n");
            Console.Write("Enter the name of the first dispatcher: ");
            string dispatcher1 = Console.ReadLine();
            Console.Write("Enter the name of the second dispatcher: ");
            string dispatcher2 = Console.ReadLine();
            Aircraft.AddDispatcher(dispatcher1);
            Aircraft.AddDispatcher(dispatcher2);
            PrintFlightInfo();
        }

        public static void PrintFlightInfo()
        {
            Console.Clear();
            Console.WriteLine("-------------------------------To control, use the following keys:-------------------------------");
            Console.WriteLine("      REDUCE SPEED:     |      INCREASE SPEED:     |   DECREASE HEIGHT:     |  INCREASE HEIGHT:     ");
            Console.WriteLine("      LEFT at 50 km / h |       RIGHT at 50 km / h |      DOWN at 250 m     |      UP at 250 m      ");
            Console.WriteLine("CTRL+LEFT at 150 km / h | CTRL+RIGHT at 150 km / h | CTRL+DOWN at 500 m     | CTRL+UP at 500 m      ");
            Console.WriteLine("------------------------------------------------------------------------------------------------");
            Console.WriteLine("  A - add dispatcher     R - remove dispatcher     Esc - output");
            Console.WriteLine("------------------------------------------------------------------------------------------------\n");
            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine("                FLIGHT SPEED: {0, 4} km / h           FLIGHT HEIGHT: {1, 5} m\n",
                Aircraft.Speed, Aircraft.Altitude);
            Console.ResetColor();
            Console.WriteLine("------------------------------------------------------------------------------------------------");
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine(Aircraft.MessageToPilot);
            Aircraft.MessageToPilot = "";
            Console.ResetColor();
            Console.WriteLine("------------------------------------------------------------------------------------------------\n");

            
            int TotalPoints = 0, count = 1;
            foreach (Dispatcher dispatcher in Aircraft.dispatchers)
            {
                Console.WriteLine("Dispatcher {0, 2}: {1, -10} | Penalty points: {2, 4} | Recommended height: {3, 5} m | {4}",
                    count, dispatcher.Name, dispatcher.Points, dispatcher.RecommendedAltitude, dispatcher.MessageToPilot);
                //dispatcher.MessageToPilot = "";
                TotalPoints += dispatcher.Points;
                ++count;
            }

            Console.WriteLine("\nTotal penalty points: {0}\n",
                TotalPoints + Aircraft.PointsFromRemovedDispatchers);
        }

        enum UserCommands
        {
            SpeedUp, SpeedUpFast, SpeedDown, SpeedDownFast,
            AltitudeUp, AltitudeUpFast, AltitudeDown, AltitudeDownFast,
            AddDispatcher, RemoveDispatcher, Exit
        };

        static UserCommands GetCommand()
        {
            do
            {
                ConsoleKeyInfo command = Console.ReadKey(true);

                switch (command.Key)
                {
                    case ConsoleKey.Escape:  
                        return UserCommands.Exit;
                    case ConsoleKey.RightArrow:
                        if (command.Modifiers == 0)
                            return UserCommands.SpeedUp;
                        else
                            return UserCommands.SpeedUpFast; 
                    case ConsoleKey.LeftArrow:
                        if (command.Modifiers == 0)
                            return UserCommands.SpeedDown;
                        else
                            return UserCommands.SpeedDownFast; 
                    case ConsoleKey.UpArrow:
                        if (command.Modifiers == 0)
                            return UserCommands.AltitudeUp;
                        else
                            return UserCommands.AltitudeUpFast; 
                    case ConsoleKey.DownArrow:
                        if (command.Modifiers == 0)
                            return UserCommands.AltitudeDown;
                        else
                            return UserCommands.AltitudeDownFast; 
                    case ConsoleKey.A:
                        return UserCommands.AddDispatcher;
                    case ConsoleKey.R:
                        return UserCommands.RemoveDispatcher;
                }

            } while (true);  
        }

        public static void Flight()
        {
            do
            {
                switch (ConsoleUserInterface.GetCommand())
                {
                    case UserCommands.SpeedUp:
                        Aircraft.Speed += 50;
                        break;
                    case UserCommands.SpeedUpFast:
                        Aircraft.Speed += 150;
                        break;
                    case UserCommands.SpeedDown:
                        Aircraft.Speed -= 50;
                        break;
                    case UserCommands.SpeedDownFast:
                        Aircraft.Speed -= 150;
                        break;
                    case UserCommands.AltitudeUp:
                        Aircraft.Altitude += 250;
                        break;
                    case UserCommands.AltitudeUpFast:
                        Aircraft.Altitude += 500;
                        break;
                    case UserCommands.AltitudeDown:
                        Aircraft.Altitude -= 250;
                        break;
                    case UserCommands.AltitudeDownFast:
                        Aircraft.Altitude -= 500;
                        break;
                    case UserCommands.AddDispatcher:
                        Console.Write("Enter the name of the new dispatcher: ");
                        string name = Console.ReadLine();
                        Aircraft.AddDispatcher(name);
                        break;
                    case UserCommands.RemoveDispatcher:
                        Console.Write("Enter the number of the dispatcher to be deleted: ");
                        int index = Convert.ToInt32(Console.ReadLine());
                        Aircraft.RemoveDispatcher(index - 1);
                        break;
                    case UserCommands.Exit:
                        Console.WriteLine("The flight is not completed.");
                        return;
                }

                ConsoleUserInterface.PrintFlightInfo();

            } while (!Aircraft.Landed);

            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine("Flight completed successfully!");
            Console.ResetColor();
            return;
        }

    }


    class Program
    {
        static void Main(string[] args)
        {
            try
            {
                ConsoleUserInterface.Start();
                ConsoleUserInterface.Flight();
                Console.WriteLine();
            }
            catch (Exception e)
            {
                ConsoleUserInterface.PrintFlightInfo();
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine(e.Message);
                Console.WriteLine("Flight failed.");
                Console.ResetColor();
            }
            finally
            {
                Console.WriteLine("The completion of the program.\n");
            }

        }

    }
}