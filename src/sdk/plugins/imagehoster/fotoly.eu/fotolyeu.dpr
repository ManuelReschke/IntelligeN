library fotolyeu;

{$R *.dres}

uses
  uPlugInInterface,
  uPlugInImageHosterClass,
  uFotolyEu in 'uFotolyEu.pas';

{$R *.res}

function LoadPlugin(var APlugIn: IImageHosterPlugIn): WordBool; safecall; export;
begin
  try
    APlugIn := TFotolyEu.Create;
    Result := True;
  except
    Result := False;
  end;
end;

exports
  LoadPlugIn name 'LoadPlugIn';

begin
end.
