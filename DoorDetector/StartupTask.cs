using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Net.Http;
using Windows.ApplicationModel.Background;
using Windows.Devices.Gpio;
using Windows.System.Threading;
using System.IO;
using System.Threading.Tasks;
using Restup.Webserver.Rest;
using Restup.Webserver.Http;
using Restup.Webserver.File;
using System.Diagnostics;

// The Background Application template is documented at http://go.microsoft.com/fwlink/?LinkID=533884&clcid=0x409

namespace DoorDetector
{
    public sealed class StartupTask : IBackgroundTask
    {
        BackgroundTaskDeferral deferral;

        private const int DOOR_PIN = 5;
        private GpioPin pin;
        private ThreadPoolTimer timer;
        DateTime lastOpenTime;
        IDoorDetectorService service;
        HttpServer httpServer;

        public async void Run(IBackgroundTaskInstance taskInstance)
        {
            Debug.WriteLine("Run");
            deferral = taskInstance.GetDeferral(); //indique au system que la tâche continue de tourner à la fin de cette méthode
            await InitGPIO(); //branchement au GPIO pour recevoir les notifications
            timer = ThreadPoolTimer.CreatePeriodicTimer(Timer_Tick, TimeSpan.FromHours(1)); //pour réalisation de tâches spécifiques
            taskInstance.Canceled += TaskInstance_Canceled; //branchement pour dispose des resources quand l'application s'arrête
        }

        private void TaskInstance_Canceled(IBackgroundTaskInstance sender, BackgroundTaskCancellationReason reason)
        {
            if (reason == BackgroundTaskCancellationReason.ServicingUpdate)
            {
                if (Log.IsEnabled(System.Diagnostics.Tracing.EventLevel.Verbose, System.Diagnostics.Tracing.EventKeywords.All))
                {
                    Log.Write("task_canceled", new { Reason = string.Format("TaskInstance_Canceled (reason:{0}", reason) });
                }
                Debug.WriteLine(string.Format("TaskInstance_Canceled (reason:{0}", reason));
                if (this.httpServer != null)
                {
                    this.httpServer.StopServer();
                }
                deferral.Complete();
            }
        }

        private async Task InitGPIO()
        {
            Debug.WriteLine("InitGPIO");
            pin = GpioController.GetDefault().OpenPin(DOOR_PIN);
            pin.ValueChanged += Pin_ValueChanged;
            pin.SetDriveMode(GpioPinDriveMode.Input);
            createDatabase();
            await startWebServer();
        }

        private async Task startWebServer()
        {
            var restRouteHandler = new RestRouteHandler();
            try
            {
                restRouteHandler.RegisterController<DoorDetectorDashBoardController>(service);
            }
            catch(Exception ex)
            {
                throw new Exception("failed to register controller", ex);
            }

            var configuration = new HttpServerConfiguration()
              .ListenOnPort(8800)
              .RegisterRoute("api", restRouteHandler)
              .RegisterRoute(new StaticFileRouteHandler(@"Web"))
              .EnableCors();

            this.httpServer = new HttpServer(configuration);
            Debug.WriteLine("BeforeStartServerAsync");
            try
            {
                await httpServer.StartServerAsync();
            }
            catch(Exception ex)
            {
                throw new Exception("failed to start WebServer", ex);
            }
        }

        private void createDatabase()
        {
            service = new DoorDetectorService(null);
            service.CheckDbStructure();
        }

        private void Pin_ValueChanged(GpioPin sender, GpioPinValueChangedEventArgs args)
        {
            if (args.Edge == GpioPinEdge.FallingEdge)
            {
                DoorOpen();
            }
            else
            {
                DoorClose();
            }
        }

        private void Timer_Tick(ThreadPoolTimer timer)
        {
            //check if db should be copied
            service.BackupDatabase();
        }

        private void DoorOpen()
        {
            var eventTime = lastOpenTime = DateTime.UtcNow;
            if (Log.IsEnabled(System.Diagnostics.Tracing.EventLevel.Verbose, System.Diagnostics.Tracing.EventKeywords.All))
            {
                Log.Write("DOOROPEN", new { EventTime = eventTime });
            }
        }

        private void DoorClose()
        {
            var eventTime = DateTime.UtcNow;
            var diff = eventTime - lastOpenTime;
            if (diff.Seconds > 0)
            {
                if (Log.IsEnabled(System.Diagnostics.Tracing.EventLevel.Verbose, System.Diagnostics.Tracing.EventKeywords.All))
                {
                    var data = new { OpenTime = lastOpenTime, CloseTime = eventTime, OpenedElasped = diff };
                    Log.Write("DOORCLOSE", data);
                }
                var ev = new DoorEvent { Id = 1, Opentime = lastOpenTime, Closetime = eventTime };
                service.AddDoorEvent(ev);
            }
            else
            {
                if (Log.IsEnabled(System.Diagnostics.Tracing.EventLevel.Verbose, System.Diagnostics.Tracing.EventKeywords.All))
                {
                    var data = new { OpenTime = lastOpenTime, CloseTime = eventTime, OpenedElasped = diff, EventMessage="this is an empty event, time between open and close is too short...." };
                    Log.Write("DOORCLOSE", data);
                }
            }
        }

    }
}
