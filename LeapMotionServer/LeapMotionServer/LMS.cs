using System;
using System.Threading;
using Leap;

using System.Net;
using System.Net.Sockets;
using System.Text;

class SampleListener : Listener
{
    private UdpClient sender;
    IPEndPoint clientIP;
    String text_to_send;
    
    private Object thisLock = new Object();

    private bool sendToClient(String message)
    {
        byte[] send_buffer = Encoding.ASCII.GetBytes(message);
        try
        {
            sender.Send(send_buffer, send_buffer.Length, clientIP);
        }
        catch (Exception send_exception)
        {
            //exception_thrown = true;
            Console.WriteLine(" Exception {0}", send_exception.Message);
            return false;
        }
        return true;
    }

    private void SafeWriteLine(String line)
    {
        lock (thisLock)
        {
            Console.WriteLine(line);
        }
    }

    public override void OnInit(Controller controller)
    {
        SafeWriteLine("Initialized");
        sender = new UdpClient();
        clientIP = new IPEndPoint(IPAddress.Parse("127.0.0.1"), 4200);
    }

    public override void OnConnect(Controller controller)
    {
        SafeWriteLine("Connected");
        controller.EnableGesture(Gesture.GestureType.TYPE_CIRCLE);
        controller.EnableGesture(Gesture.GestureType.TYPE_KEY_TAP);
        controller.EnableGesture(Gesture.GestureType.TYPE_SCREEN_TAP);
        controller.EnableGesture(Gesture.GestureType.TYPE_SWIPE);
    }

    public override void OnDisconnect(Controller controller)
    {
        //Note: not dispatched when running in a debugger.
        SafeWriteLine("Disconnected");
    }

    public override void OnExit(Controller controller)
    {
        SafeWriteLine("Exited");
    }
    
    string gestureInfo = "X00";
    long latestGestureFrame = 0;
    Gesture latestGesture;
    int gestureCounter = 0;
    string thumb = "(0,0,0)", index = "(0,0,0)", palm = "(0,0,0)";
    int frameSkip = 2;
    public override void OnFrame(Controller controller)
    {
        // Get the most recent frame and report some basic information
        Frame frame = controller.Frame();

        SafeWriteLine("Frame id: " + frame.Id
                    + ", timestamp: " + frame.Timestamp
                    + ", hands: " + frame.Hands.Count
                    + ", fingers: " + frame.Fingers.Count
                    + ", tools: " + frame.Tools.Count
                    + ", gestures: " + frame.Gestures().Count);

        

        StringBuilder dataToSend = new StringBuilder();
        thumb = "(0,0,0)"; index = "(0,0,0)"; palm = "(0,0,0)";
        
        foreach (Hand hand in frame.Hands)
        {
            SafeWriteLine("  Hand id: " + hand.Id
                        + ", palm position: " + hand.PalmPosition);
            // Get the hand's normal vector and direction
            Vector normal = hand.PalmNormal;
            Vector direction = hand.Direction;


            palm = hand.PalmPosition.ToString();
            //dataToSend.Append("HPOS_" + hand.Id + " " + hand.PalmPosition.ToString());

            //sendToClient("HPOS_" + hand.Id + " " + hand.PalmPosition.ToString());// + " " + normal.y.ToString("0.0000") + " " + normal.z.ToString("0.0000"));

            /*
            // Calculate the hand's pitch, roll, and yaw angles
            SafeWriteLine("  Hand pitch: " + direction.Pitch * 180.0f / (float)Math.PI + " degrees, "
                        + "roll: " + normal.Roll * 180.0f / (float)Math.PI + " degrees, "
                        + "yaw: " + direction.Yaw * 180.0f / (float)Math.PI + " degrees");
            */
            /*
            // Get the Arm bone
            Arm arm = hand.Arm;
            SafeWriteLine("  Arm direction: " + arm.Direction
                        + ", wrist position: " + arm.WristPosition
                        + ", elbow position: " + arm.ElbowPosition);
            */
            // Get fingers
            foreach (Finger finger in hand.Fingers)
            {
                //SafeWriteLine("    Finger id: " + finger.Id
                 //           + ", " + finger.Type.ToString()
                 //           + ", length: " + finger.Length
                 //           + "mm, width: " + finger.Width + "mm");

                if (finger.Type == Finger.FingerType.TYPE_THUMB)
                    thumb = finger.TipPosition.ToString();
                else if(finger.Type == Finger.FingerType.TYPE_INDEX)
                    index = finger.TipPosition.ToString();

                if (thumb != "(0,0,0)" && index != "(0,0,0)")
                    break;

                // Get finger bones
                /*
                Bone bone;
                foreach (Bone.BoneType boneType in (Bone.BoneType[])Enum.GetValues(typeof(Bone.BoneType)))
                {
                    bone = finger.Bone(boneType);
                    SafeWriteLine("      Bone: " + boneType
                                + ", start: " + bone.PrevJoint
                                + ", end: " + bone.NextJoint
                                + ", direction: " + bone.Direction);
                }
                 */
            }

            //sendToClient(dataToSend.Append(thumb).Append(index).ToString());

        }

        /*
        // Get tools
        foreach (Tool tool in frame.Tools)
        {
            SafeWriteLine("  Tool id: " + tool.Id
                        + ", position: " + tool.TipPosition
                        + ", direction " + tool.Direction);
        }
        */
        ///*
        // Get gestures
        GestureList gestures = frame.Gestures();
        
        if(gestures.Count > 0 && frame.Id - latestGestureFrame > 90 )
        {
            latestGestureFrame = frame.Id;
      
            
            for (int i = 0; i < gestures.Count; i++)
            {
                Gesture gesture = gestures[i];
                
                
                /*
                if(gesture.Type == latestGesture.Type)
                {
                    gestureCounter++;
                }
                else
                {
                    latestGesture = gesture;
                    gestureCounter = 0;
                }
                */
                if (gesture.DurationSeconds > 0.25 || gesture.Type == Gesture.GestureType.TYPE_SWIPE) //(gesture.State == Gesture.GestureState.STATE_UPDATE)/
                switch (gesture.Type)
                {
                    case Gesture.GestureType.TYPE_CIRCLE:
                        CircleGesture circle = new CircleGesture(gesture);

                        
                        
                        // Calculate clock direction using the angle between circle normal and pointable
                        String clockwiseness;
                        if (circle.Pointable.Direction.AngleTo(circle.Normal) <= Math.PI / 2)
                        {
                            //Clockwise if angle is less than 90 degrees
                            clockwiseness = "clockwise";
                            gestureInfo = "c" + frame.Id;
                        }
                        else
                        {
                            clockwiseness = "counterclockwise";
                            gestureInfo = "C" + frame.Id;
                        }

                        float sweptAngle = 0;

                        // Calculate angle swept since last frame
                        if (circle.State != Gesture.GestureState.STATE_START)
                        {
                            CircleGesture previousUpdate = new CircleGesture(controller.Frame(1).Gesture(circle.Id));
                            sweptAngle = (circle.Progress - previousUpdate.Progress) * 360;
                        }

                        SafeWriteLine("  Circle id: " + circle.Id
                                       + ", " + circle.State
                                       + ", progress: " + circle.Progress
                                       + ", radius: " + circle.Radius
                                       + ", angle: " + sweptAngle
                                       + ", " + clockwiseness);
                        break;
                    case Gesture.GestureType.TYPE_SWIPE:
                        SwipeGesture swipe = new SwipeGesture(gesture);

                        gestureInfo = "S" + frame.Id;

                        SafeWriteLine("  Swipe id: " + swipe.Id
                                       + ", " + swipe.State
                                       + ", position: " + swipe.Position
                                       + ", direction: " + swipe.Direction
                                       + ", speed: " + swipe.Speed);
                        break;
                    case Gesture.GestureType.TYPE_KEY_TAP:
                        KeyTapGesture keytap = new KeyTapGesture(gesture);

                        gestureInfo = "K" + frame.Id;

                        SafeWriteLine("  Tap id: " + keytap.Id
                                       + ", " + keytap.State
                                       + ", position: " + keytap.Position
                                       + ", direction: " + keytap.Direction);
                        break;
                    case Gesture.GestureType.TYPE_SCREEN_TAP:
                        ScreenTapGesture screentap = new ScreenTapGesture(gesture);

                        gestureInfo = "T" + frame.Id;

                        SafeWriteLine("  Tap id: " + screentap.Id
                                       + ", " + screentap.State
                                       + ", position: " + screentap.Position
                                       + ", direction: " + screentap.Direction);
                        break;
                    default:
                        SafeWriteLine("  Unknown gesture type.");
                        break;
                }
            }
        }

        if (frame.Id % frameSkip == 0)
        {
            dataToSend.Append(gestureInfo + " ").Append(palm).Append(thumb).Append(index);
            sendToClient(dataToSend.ToString());
        }
        //*/
        if (!frame.Hands.IsEmpty || !frame.Gestures().IsEmpty)
        {
            SafeWriteLine("");
        }
    }
}

class Sample
{
    public static void Main()
    {
        // Create a sample listener and controller
        SampleListener listener = new SampleListener();
        Controller controller = new Controller();

        controller.SetPolicy(Leap.Controller.PolicyFlag.POLICY_BACKGROUND_FRAMES);

        // Have the sample listener receive events from the controller
        controller.AddListener(listener);

        //creating socket
        //   UdpClient sendingClient = new UdpClient();
        //sendingClient.Client.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReuseAddress, true);
        //sendingClient.Connect("localhost", 4200);

        //Socket sending_socket = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);
        //   IPAddress send_to_address = IPAddress.Parse("127.0.0.1");//localhost, for broadcast use ("192.168.2.255");
        //   IPEndPoint sending_end_point = new IPEndPoint(send_to_address, 4200);

        /*
        while(true)
        {



           // String text_to_send = "VR_TRACKER_SENSOR_0 42 24 66 0 0 0 0 0 0 1 2 3 4";
            //byte[] send_buffer = Encoding.ASCII.GetBytes(text_to_send);

            //Console.WriteLine("sending to address: {0} port: {1}",  sending_end_point.Address, sending_end_point.Port);
        
        
            try
            {
                //sending_socket.SendTo(send_buffer, sending_end_point);
                //sendingClient.Send(send_buffer, send_buffer.Length, sending_end_point);
            }
            catch (Exception send_exception)
            {
                //exception_thrown = true;
                Console.WriteLine(" Exception {0}", send_exception.Message);
            }
          
            Console.ReadLine();
            //System.Threading.Thread.Sleep(1000);
        }
        */
        // Keep this process running until Enter is pressed
        Console.WriteLine("Press Enter to quit...");
        Console.ReadLine();

        // Remove the sample listener when done
        controller.RemoveListener(listener);
        controller.Dispose();
    }
}
