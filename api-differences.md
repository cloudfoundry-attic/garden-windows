# Garden windows api limitation

| Method name                      | Implementation                                                      |
|----------------------------------+---------------------------------------------------------------------+
| Backend#Start                    | Not implmented                                                      |
| Backend#Stop                     | Not implemented                                                     |
| Backend#GraceTime                | Not implemented                                                     |
| Backend#Ping                     |                                                                     |
| Backend#Capacity                 | (MaxContainers = 256)                                               |
| Backend#Create                   | (All container specs are ignored except `Handle` and `Properties`)  |
| Backend#Destroy                  | Implemented                                                         |
| Backend#Containers               | Implemented                                                         |
| Backend#Lookup                   | (dummy returns a container without checking if the handle exists)   |
| Backend#BulkInfo                 | Implemented                                                         |
| Backend#BulkMetrics              | In progress                                                         |
| Container#Handle                 | Implemented                                                         |
| Container#Stop                   | (the `kill` flag is ignored, kill is the default)                   |
| Container#Info                   | (Only `MappedPorts`, `Properties` and `ExternalIP`                  |
| Container#StreamIn               | Implemented                                                         |
| Container#StreamOut              | Implemented                                                         |
| Container#LimitBandwidth         | Not Implemented                                                     |
| Container#CurrentBandwidthLimits | Not Implemented                                                     |
| Container#LimitCPU               | In progess (see notes below)                                        |
| Container#CurrentCPULimits       | In progress                                                         |
| Container#LimitDisk              | In progress                                                         |
| Container#CurrentCPULimits       | In progress                                                         |
| Container#LimitMemroy            | Implemented                                                         |
| Container#CurrentMemoryLimits    | Implemented                                                         |
| Container#NetIn                  | (TODO: what are the limitations ?)                                  |
| Container#NetOut                 | (Except ICMP. ICMP is currently blocked for all containers)         |
| Container#Run                    | (All flags are ignored except `Path`, `Args` and `Env`, see notes)  |
| Container#Attach                 | Not implemented                                                     |
| Container#Metrics                | In progress (TODO: document the limitations)                        |
| Container#Properties             | Implemented                                                         |
| Container#Property               | Implemented                                                         |
| Container#SetProperty            | Implemented                                                         |
| Container#RemoveProperty         | Implemented                                                         |
| Processes#ID                     | Implemented                                                         |
| Processes#Wait                   | Implemented                                                         |
| Processes#SetTTY                 | Not Implemented                                                     |
| Processes#Signal                 | Implemented (the signal argument is ignored, the process is killed) |

# Limitations of Container#LimitCPU

As opposed to garden linux, on windows we **will probably** (this
isn't implemented yet) use hard caps instead of cpu shares. The hard
cap will be calculated based on the relative number of shares given to
the container. This means that two containers with 8/2 will have their
caps set to 80% and 20%, respectively. The container with 20% cap will
not be able to use more than 20% of the cpu even if the other
container was idle (this isn't the case with cpu shares on Linux).

# Limitations of Container#Run

1. No support for `Privileged` contianers (the flag is ignored)
2. `User` flag is ignored (`garden-windows` creates a new user for each container)
