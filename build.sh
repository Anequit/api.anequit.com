#!/bin/bash

dotnet build ./src/API -c Release -o bin
cp scripts/*.* bin