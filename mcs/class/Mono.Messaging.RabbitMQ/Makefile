thisdir = class/Mono.Messaging.RabbitMQ
SUBDIRS = 
include ../../build/rules.make

LIBRARY = WineMono.Messaging.RabbitMQ.dll
LIB_REFS = System System.Messaging RabbitMQ.Client
KEYFILE = ../mono.pub
LIB_MCS_FLAGS = -nowarn:618

TEST_MCS_FLAGS = $(LIB_MCS_FLAGS) -nowarn:0618 -nowarn:219 -nowarn:169
TEST_LIB_REFS =

DEP_LIBS = $(LIB_REFS) WineMono.Messaging 
DEP_DIRS = $(LIB_REFS) Mono.Messaging 

include ../../build/library.make

export MONO_MESSAGING_PROVIDER=Mono.Messaging.RabbitMQ.RabbitMQMessagingProvider,Mono.Messaging.RabbitMQ
