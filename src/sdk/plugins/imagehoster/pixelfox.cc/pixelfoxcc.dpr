library pixelfoxcc;

{$R *.dres}

uses
  uPlugInInterface,
  uPlugInImageHosterClass,
  uPixelfoxCc in 'uPixelfoxCc.pas';

{$R *.res}

function LoadPlugin(var APlugIn: IImageHosterPlugIn): WordBool; safecall; export;
begin
  try
    APlugIn := TPixelfoxCc.Create;
    Result := True;
  except
    Result := False;
  end;
end;

exports
  LoadPlugIn name 'LoadPlugIn';

begin
end.
