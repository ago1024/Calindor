cd..
FOR /F "tokens=1* delims= " %%G IN ('cd') DO SET "top_dir=%%G"
cd release
FOR /F "tokens=1 delims= " %%G IN ('Calindor.exe /pv') DO SET "calindor_version=%%G"
SET "dest_dir=Calindor_%calindor_version%"
cd..
cd setup
rmdir %dest_dir% /S /Q
mkdir %dest_dir%
cd %dest_dir%
mkdir maps
mkdir storage
cd..
cd..
copy release\Calindor.exe setup\%dest_dir%
copy setup\server_config_default.xml setup\%dest_dir%\server_config.xml
copy doc\Installation.txt setup\%dest_dir%
copy doc\License.txt setup\%dest_dir%
cd "%top_dir%\setup"
7za a -r -tzip %dest_dir%.zip %dest_dir%\*