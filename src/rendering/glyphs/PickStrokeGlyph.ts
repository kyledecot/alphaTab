import { PickStroke } from '@src/model/PickStroke';
import { ICanvas } from '@src/platform/ICanvas';
import { MusicFontGlyph } from '@src/rendering/glyphs/MusicFontGlyph';
import { MusicFontSymbol } from '@src/rendering/glyphs/MusicFontSymbol';
import { NoteHeadGlyph } from '@src/rendering/glyphs/NoteHeadGlyph';

export class PickStrokeGlyph extends MusicFontGlyph {
    private _shiftDown: boolean;

    public constructor(x: number, y: number, pickStroke: PickStroke, shiftDown: boolean = false) {
        super(x, y, NoteHeadGlyph.GraceScale, PickStrokeGlyph.getSymbol(pickStroke));
        this._shiftDown = shiftDown;
    }

    public doLayout(): void {
        this.width = 9 * this.scale;
        this.height = 13 * this.scale;
    }

    public paint(cx: number, cy: number, canvas: ICanvas): void {
        if(this._shiftDown) {
            cy += this.height;
        }
        super.paint(cx, cy, canvas);
    }

    private static getSymbol(pickStroke: PickStroke): MusicFontSymbol {
        switch (pickStroke) {
            case PickStroke.Up:
                return MusicFontSymbol.StringsUpBow;
            case PickStroke.Down:
                return MusicFontSymbol.StringsDownBow;
            default:
                return MusicFontSymbol.None;
        }
    }
}
