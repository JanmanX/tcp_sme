ffmpeg -i $1 -vf "select=eq(n\,0)" -q:v 3 "thumbnail.png"
