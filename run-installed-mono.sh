#!/bin/sh

PREFIX=$1
shift
ARGS=$*

MONO=$PREFIX/runtime/mono-wrapper
MONO_LIBDIR=$PREFIX/mcs/class/lib/net_4_x

export MONO_PATH=$MONO_LIBDIR
exec $MONO $ARGS
