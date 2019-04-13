// Defines the macros for the logging library
#define LOGGER this
#define TRACE(x) SimulationOnly(() =>{Logging.log.Trace(x);})
#define DEBUG(x) SimulationOnly(() =>{Logging.log.Debug(x);})
#define INFO(x) SimulationOnly(() =>{Logging.log.Info(x);})
#define WARN(x) SimulationOnly(() =>{Logging.log.Warn(x);})
#define ERROR(x) SimulationOnly(() =>{Logging.log.Error(x);})
#define FATAL(x) SimulationOnly(() =>{Logging.log.Fatal(x);})