#!/bin/bash
cpu_usage=$(printf "%d%%" $((100-$(vmstat 1 2|tail -1|awk '{print $15}'))))
mem_usage=$(free -m | awk 'NR==2{ printf "%.2f%%", $3*100/$2 }')
echo "$cpu_usage"
echo "$mem_usage"