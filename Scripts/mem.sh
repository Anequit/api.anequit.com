#!/bin/bash
free -m | awk 'NR==2{ printf "Memory Usage: %.2f%%\n", $3*100/$2 }'