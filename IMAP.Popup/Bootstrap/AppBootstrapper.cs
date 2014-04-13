using Caliburn.Micro;
using Castle.MicroKernel.Registration;
using Castle.Windsor;
using Hardcodet.Wpf.TaskbarNotification;
using IMAP.Popup.Models;
using IMAP.Popup.ViewModels;
using Raven.Client;
using Raven.Client.Embedded;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Raven.Database.Tasks;

namespace IMAP.Popup.Bootstrap
{
    public class AppBootstrapper : Bootstrapper<PopupIconViewModel>, IDisposable
	{
		private WindsorContainer _container;
        private TaskbarIcon _taskbarIcon;
        private readonly ManualResetEventSlim _taskbarIconInitializedEvent = new ManualResetEventSlim();

		protected override void Configure()
		{
            _container = new WindsorContainer();

		    _container.AddFacility<EventRegistrationFacility>();

            _container.Register(
                Component.For<TaskbarIcon>().UsingFactoryMethod((kernel, creationContext) =>
                {
                    _taskbarIconInitializedEvent.Wait();
                    return _taskbarIcon;
                }),
                Component.For<IWindowManager>().ImplementedBy<WindowManager>().LifestyleSingleton(),
                Component.For<IEventAggregator>().ImplementedBy<EventAggregator>().LifestyleSingleton(),
                Component.For<IDocumentStore>().UsingFactoryMethod<EmbeddableDocumentStore>((kernel, creationContext) =>
                {
                    var store = new EmbeddableDocumentStore();
                    store.Initialize();

                    return store;
                }).LifestyleSingleton(),
                Classes.FromThisAssembly()
                       .InSameNamespaceAs<ConfigurationViewModel>()
                       .WithServiceSelf()
                       .WithServiceDefaultInterfaces()
                       .LifestyleSingleton(),
                Classes.FromThisAssembly()
                       .InSameNamespaceAs<PersistanceModel>()
                       .WithServiceSelf()
                       .WithServiceDefaultInterfaces()
                       .LifestyleSingleton());

            //pre-initialize some of models
			System.Threading.Tasks.Task.Run(() =>
			{
				_container.Resolve<IDocumentStore>();
				_container.Resolve<PopupIconModel>();
				_container.Resolve<PopupIconViewModel>();
			});
		}

        protected override void OnStartup(object sender, System.Windows.StartupEventArgs e)
        {
            _taskbarIcon = (TaskbarIcon)System.Windows.Application.Current.FindResource("TaskbarIcon");
            _taskbarIconInitializedEvent.Set();
        }        

		protected override object GetInstance(Type serviceType, string key)
		{
            if (string.IsNullOrEmpty(key))
            {
                return _container.Resolve(serviceType);
            }
		    return _container.Resolve(key, serviceType);
		}

		protected override IEnumerable<object> GetAllInstances(Type serviceType)
		{
			return _container.ResolveAll(serviceType).Cast<object>();
		}

        public void Dispose()
        {
            _taskbarIcon.Dispose();
        }
    }
}