using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace Prism.Common
{
    public class EventDistributor
    {
        private readonly object _publisher;
        private readonly EventInfo _eventInfo;
        private readonly Delegate _handler;
        public EventDistributor(object publisher, string eventName, Action<object, EventArgs> action)
        {
            if (publisher == null) throw new ArgumentNullException(nameof(publisher));
            if (eventName == null) throw new ArgumentNullException(nameof(eventName));
            if (action == null) throw new ArgumentNullException(nameof(action));

            _publisher = publisher;

            _eventInfo = publisher.GetType().GetRuntimeEvent(eventName);
            if (_eventInfo == null)
            {
                throw new ArgumentException(
                    $"No matching event '{eventName}' on publisher type '{publisher.GetType().Name}'");
            }

            var eventParameters = 
                _eventInfo
                    .EventHandlerType
                    .GetRuntimeMethods().First(m => m.Name == "Invoke")
                    .GetParameters()
                    .Select(p => Expression.Parameter(p.ParameterType))
                    .ToArray();

            var actionInvoke = action.GetType()
                .GetRuntimeMethods().First(m => m.Name == "Invoke");

            _handler = 
                Expression.Lambda(
                    _eventInfo.EventHandlerType,
                    Expression.Call(Expression.Constant(action), actionInvoke, eventParameters[0], eventParameters[1]),
                    eventParameters).Compile();

            _eventInfo.AddEventHandler(publisher, _handler);
        }

        public void Stop()
        {
            _eventInfo.RemoveEventHandler(_publisher, _handler);
        }
    }
}
