#!/bin/bash
free -m | awk 'NR==2{ printf "Memory Usage: %.2f%%", $3*100/$2 }'