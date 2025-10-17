using AUTD3Sharp;
using AUTD3Sharp.Gain;
using AUTD3Sharp.Modulation;
using AUTD3Sharp.Utils;
using AUTD3Sharp.Link;
using static AUTD3Sharp.Units;

using var autd = Controller.Open([new AUTD3(pos: Point3.Origin, rot: Quaternion.Identity)], new EtherCrab(
        (idx, status) =>
        {
                Console.Error.WriteLine($"Device[{idx}]: {status}");
                if (status == Status.Lost)
                        // You can also wait for the link to recover, without exiting the process
                        Environment.Exit(-1);
        }, option: new EtherCrabOption()));

autd.Send((new Sine(freq: 150f * Hz, option: new SineOption()), new Focus(pos: autd.Center() + new Vector3(0f, 0f, 150f), option: new FocusOption())));

Console.ReadKey(true);

autd.Close();
