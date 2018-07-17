# ClassiCube-Tools
Some user unfriendly tools for ClassiCube

## mono-debug-mem.c
Script I used to try to work out memory leaking on mono. To compile the profiler
```
gcc -fPIC -shared -o libmono-profiler-sample.so mono-debug-mem.c `pkg-config --cflags mono-2`
```
then move the .so file to
```
/usr/lib/libmono-profiler-sample.so
```

then run your app as
```
mono --profile=sample MCGalaxyCLI.exe
```
