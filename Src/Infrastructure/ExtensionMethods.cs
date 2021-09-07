using Desktop.Model;
using Syncfusion.Windows.Controls.Gantt;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;

namespace Desktop.Infrastructure
{
    public static class ExtensionMethods
    {
         
        public static ObservableCollection<TaskDetails> GetTimeLine(this IPAddressAbuse data)
        {


            var result = new List<TaskDetails>(2);
            int taskId = 1;

            TaskDetails portAttack = null;
            TaskDetails webAttack = null;

            if (data.PortBasedAttacks.Count > 0)
            {
                portAttack = new TaskDetails() { TaskName = "Port based Attack", TaskId = taskId++ };
                portAttack.StartDate = data.PortBasedAttacks.Min(m => m.FirstDetectedOn);
                portAttack.FinishDate = data.PortBasedAttacks.Max(m => m.FirstDetectedOn);
                result.Add(portAttack);
            }
            if (data.WebBasedAttacks.Count > 0)
            {
                webAttack = new TaskDetails() { TaskName = "HTTP based Attack", TaskId = taskId++ };
                webAttack.StartDate = data.WebBasedAttacks.Min(m => m.RequestedUtc);
                webAttack.FinishDate = data.WebBasedAttacks.Max(m => m.RequestedUtc);
                result.Add(webAttack);
            }

            if (portAttack is not null)
            {
                var group = data.PortBasedAttacks.GroupBy(g => g.PortName);
                foreach (var port in group.OrderBy(o => o.Key))
                {
                    //var resource = ResourceCollection.Where(f => f.Name == port.Key);
                    TaskDetails attackedPort = new TaskDetails()
                    {
                        StartDate = port.Min(m => m.FirstDetectedOn),
                        FinishDate = port.Max(m => m.LastDetection),
                        TaskName = $"{port.Count():N0} penetration attempts on {port.Key}",
                        TaskId = taskId++,
                        
                    };
                    attackedPort.Duration = attackedPort.FinishDate - attackedPort.StartDate;
                    
                    portAttack.Child.Add(attackedPort);

                    foreach (var attac in port.OrderBy(o => o.FirstDetectedOn))
                    {
                        var item = new TaskDetails()
                        {
                            TaskId = taskId++,
                            TaskName = $"Port {attac.PortName} triggered {attac.Communications:N0} times returned {attac.PayloadInfo}",
                            StartDate = attac.FirstDetectedOn,
                            FinishDate = attac.LastDetection,
                            Duration = attac.LastDetection - attac.FirstDetectedOn,
                        };
                        attackedPort.FinishDate = item.FinishDate;
                        attackedPort.Duration = attackedPort.FinishDate - attackedPort.StartDate;
                        attackedPort.Child.Add(item);
                    }

                    attackedPort.Duration = attackedPort.FinishDate - attackedPort.StartDate;

                }
            }

            if (webAttack is not null)
            {              

                var group = data.WebBasedAttacks.GroupBy(g => g.Uri.AbsoluteUri);
                foreach (var endpoint in group)
                {
                    //var resource = ResourceCollection.Where(f => f.Name == endpoint.Key);
                    TaskDetails attackedEndpoint = new TaskDetails()
                    {
                        StartDate = endpoint.Min(m => m.RequestedUtc),
                        FinishDate = endpoint.Max(m => m.RequestedUtc),
                        TaskName = $"{endpoint.Count():N0} penetration attempts on {endpoint.Key}",
                        TaskId = taskId++,                        
                    };

                    attackedEndpoint.Duration = attackedEndpoint.FinishDate - attackedEndpoint.StartDate;

                    webAttack.Child.Add(attackedEndpoint);
                    foreach (var request in endpoint)
                    {
                        var item = new TaskDetails()
                        {
                            TaskId = taskId++                           ,
                            TaskName = $"{request.Method} {request.Incidents.Count:N0} Incidents triggered {request.Triggered}"                           ,
                            StartDate = request.RequestedUtc                           ,
                            FinishDate = request.RequestedUtc.AddMilliseconds(10),                            
                        };


                        if (request.Evaluations is not null && request.Evaluations.Count > 0)
                        {
                            foreach (var statck in request.Evaluations.OrderBy(o => o.Evaluated))
                            {
                                item.Child.Add(new TaskDetails()
                                {
                                    TaskId = taskId++,
                                    FinishDate = statck.Evaluated,
                                    StartDate = request.RequestedUtc,
                                    Duration = statck.Evaluated - request.RequestedUtc,
                                    TaskName = $"Rule {statck.Reasons }: allowed = {statck.Allowed} actual={statck.Actual}",
                                   
                                }) ;

                                item.FinishDate = statck.Evaluated;
                                attackedEndpoint.FinishDate = statck.Evaluated;
                            }
                            
                        }                       

                        
                        attackedEndpoint.Child.Add(item);                           
                        item.Duration = item.FinishDate - item.StartDate;
                    }
                    attackedEndpoint.FinishDate = attackedEndpoint.Child[^1].FinishDate;
                    attackedEndpoint.Duration = attackedEndpoint.Child[^1].FinishDate- attackedEndpoint.Child[0].StartDate;
                }


            }
            return new ObservableCollection<TaskDetails>(result);
        }
    }
}
