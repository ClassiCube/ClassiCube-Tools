//#include <config.h>
#include <mono/metadata/mono-gc.h>
#include <mono/metadata/profiler.h>
#include <unistd.h>
#include <stdio.h>
//#include <glib.h>
typedef int gboolean;

struct _MonoProfiler {
        MonoProfilerHandle handle;
};

static MonoProfiler profiler;
int big_count = 0;
MonoObject* big[1000];

static int gc_find_big(MonoObject *obj, MonoClass *klass, uintptr_t size, uintptr_t num,
        MonoObject **refs, uintptr_t *offsets, void *data) {
        // account for object alignment in the heap 
        size += 7; size &= ~7;
        if (size < 1000 * 1000) return 0;
        
        big[big_count] = obj; big_count++;
        puts("  ## BIG OBJECT DETECTED ##");
		
        const char *name = mono_class_get_name (klass);
        const char *nspace = mono_class_get_namespace (klass);
        printf("%s.%s of %i bytes\n", nspace, name, size);
        return 0;
}

static int gc_dump_big_refs(MonoObject *obj, MonoClass *klass, uintptr_t size, uintptr_t num,
        MonoObject **refs, uintptr_t *offsets, void *data) {    
		if (num == 0) return 0;
		int i, j;
		
		for (i = 0; i < num; i++) {
			for (j = 0; j < big_count; j++) {
				if (refs[i] == big[j]) { goto found; }
			}
		}
		
		return 0;    
    found:
        puts("  ## REFERENCING BIG OBJECT DETECTED ##");
		
        const char *name = mono_class_get_name (klass);
        const char *nspace = mono_class_get_namespace (klass);
        printf("    %s.%s\n", nspace, name);
        return 0;
}

static void gc_event(MonoProfiler *profiler, MonoProfilerGCEvent ev, uint32_t generation, gboolean is_serial) {
        switch (ev) {
        case MONO_GC_EVENT_PRE_START_WORLD:
				//mono_gc_walk_heap(0, gc_dump_big, N)
				puts("==== PERFORMING GC");
				big_count = 0;
                mono_gc_walk_heap(0, gc_find_big, NULL);
				mono_gc_walk_heap(0, gc_dump_big_refs, NULL);
				
        }
}

MONO_API void mono_profiler_init_sample(const char *desc) {
        profiler.handle = mono_profiler_create (&profiler);
        mono_profiler_set_gc_event_callback (profiler.handle, gc_event);
        puts("=== PROFILER ATTACHED ===");
}

MONO_API void mono_profiler_start(const char* desc)  {
        mono_profiler_init_sample(desc);
}



/*
static int gc_dump_big(MonoObject *obj, MonoClass *klass, uintptr_t size, uintptr_t num,
        MonoObject **refs, uintptr_t *offsets, void *data) {
        // account for object alignment in the heap 
        size += 7; size &= ~7;

        //log 1 MB big objects
        if (size > 1000 * 1000) {
                puts("## BIG OBJECT DETECTED ##");
                const char *name;
                const char *nspace;
                name = mono_class_get_name (klass);
                nspace = mono_class_get_namespace (klass);
                printf("%s.%s of %i bytes", name, nspace, size);

                // print objects this is referencing
                if (refs == 0) return 0;
                int i ;
                for (i = 0; refs[i] != 0; i++) {
                        puts("I am referencing a: ");
                        klass = mono_object_get_class(refs[i]);
                        name = mono_class_get_name (klass);
                        nspace = mono_class_get_namespace (klass);
                        printf("%s.%s", name, nspace);
                }
        }
        return 0;
}
*/
