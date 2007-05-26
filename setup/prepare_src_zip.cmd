cd..
FOR /F "tokens=1* delims= " %%G IN ('cd') DO SET "top_dir=%%G"
cd release
FOR /F "tokens=1 delims= " %%G IN ('Calindor.exe /pv') DO SET "calindor_version=%%G"
SET "dest_dir=calindor-%calindor_version%-src"
cd..
cd setup
rmdir %dest_dir% /S /Q
mkdir %dest_dir%
cd..
xcopy src\* setup\%dest_dir%\src\ /E
cd setup
cd %dest_dir%
cd src
cd server
rmdir bin /S /Q
rmdir obj /S /Q
del *.user /Q
cd..
cd csu
rmdir bin /S /Q
rmdir obj /S /Q
del *.user /Q
cd..
cd..
cd..
cd..
copy "doc\release-notes\calindor-%calindor_version%-release-notes.txt" setup\%dest_dir%\
copy doc\installation.txt setup\%dest_dir%\
copy doc\compilation.txt setup\%dest_dir%\
copy doc\license.txt setup\%dest_dir%\
copy doc\features.txt setup\%dest_dir%\
copy doc\commands.txt setup\%dest_dir%\
copy setup\server_config_default.xml setup\%dest_dir%\src\server\server_config.xml /Y
cd "%top_dir%\setup"
7za a -r -tzip %dest_dir%.zip %dest_dir%\*