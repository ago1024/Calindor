cd..
FOR /F "tokens=1* delims= " %%G IN ('cd') DO SET "top_dir=%%G"
cd release
FOR /F "tokens=1 delims= " %%G IN ('Calindor.exe /pv') DO SET "calindor_version=%%G"
SET "dest_dir=calindor-%calindor_version%-bin"
cd..
cd setup
rmdir %dest_dir% /S /Q
mkdir %dest_dir%
cd %dest_dir%
mkdir maps
mkdir storage
cd..
cd..
copy release\calindor.exe setup\%dest_dir%\
copy release\csu.exe setup\%dest_dir%\
copy setup\server_config_default.xml setup\%dest_dir%\server_config.xml
copy "doc\release-notes\calindor-%calindor_version%-release-notes.txt" setup\%dest_dir%\
copy doc\installation.txt setup\%dest_dir%\
copy doc\license.txt setup\%dest_dir%\
copy doc\features.txt setup\%dest_dir%\
copy doc\commands.txt setup\%dest_dir%\
cd "%top_dir%\setup"
7za a -r -tzip %dest_dir%.zip %dest_dir%\*