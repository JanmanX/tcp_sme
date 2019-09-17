#!/usr/bin/env bash
if [ -z "$1" ]; then
    echo "Supply path argument"
    exit 1
fi
case "$1" in
    */)
        ROOTPATH="$1"
        ;;
    *)
        ROOTPATH="$1/"
        ;;
esac
DUMPSYSTEMSTATE="DumpSystemState";
DUMPSTATE="DumpState"
echo $ROOTPATH

# Convert the files
sh ./dotToImage.sh "$ROOTPATH$DUMPSTATE"
sh ./neatoToImage.sh "$ROOTPATH$DUMPSYSTEMSTATE"
pushd $ROOTPATH;
SIMULATIONGRAPH="$DUMPSTATE/out.mp4"
SYSTEMGRAPH="$DUMPSYSTEMSTATE/out.mp4"
eval $(ffprobe -v quiet -show_format -of flat=s=_ -show_entries stream=height,width -sexagesimal "$SIMULATIONGRAPH");
SimGraphW=${streams_stream_0_width};
SimGraphH=${streams_stream_0_height};
eval $(ffprobe -v quiet -show_format -of flat=s=_ -show_entries stream=height,width -sexagesimal "$SYSTEMGRAPH");
SysGraphW=${streams_stream_0_width};
SysGraphH=${streams_stream_0_height};
echo $SimGraphH,$SysGraphH
ffmpeg \
       -i "$SIMULATIONGRAPH" \
       -i "$SYSTEMGRAPH" \
       -filter_complex "
       [0:v]pad=iw:'max($SimGraphH , $SysGraphH)':0:0:white[left];
       [1:v]pad=iw:'max($SimGraphH , $SysGraphH)':0:0:white[right];
       [left][right]hstack=inputs=2[vid]" \
       -map [vid] \
       -c:v libx264 \
       -crf 18 \
       "output.mp4"
popd