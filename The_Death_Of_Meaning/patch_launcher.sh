#!/bin/bash
awk '
/public sealed class AnchorLauncher : NetworkBehaviour/ {
    print $0
    next
}
/public bool AnclaActiva \{ get; internal set; \}/ {
    next
}
/\{/ && !done {
    print "    {"
    print "        public bool AnclaActiva { get; internal set; }"
    done = 1
    next
}
{ print $0 }
' ./Assets/_Project/Scripts/Unity/Combat/AnchorLauncher.cs > tmp.cs
mv tmp.cs ./Assets/_Project/Scripts/Unity/Combat/AnchorLauncher.cs
